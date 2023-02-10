using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.MultiplayerModels;
using FirstLight.FLogger;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using SRF;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Hanldles matchmaking flow, that is, the flow for players to find another players to join a match.
	/// The flow is:
	/// - Player obtains a "Matchmaking Ticket"
	/// - Players either subscribes to notifications or polls on this ticket to see when its ready
	/// - When its ready player will have a identifier to know which game he needs to join.
	/// </summary>
	public interface IMatchmakingService
	{
		/// <summary>
		/// Leaves all matchmaking tickets that are being waited
		/// </summary>
		public void LeaveMatchmaking();

		/// <summary>
		/// Obtains a given matchmaking ticket the ticket information
		/// </summary>
		public void GetTicket(string ticket, Action<GetMatchmakingTicketResult> callback);

		/// <summary>
		/// Get a list of all my current active tickets
		/// </summary>
		public void GetMyTickets(Action<ListMatchmakingTicketsForPlayerResult> callback);

		/// <summary>
		/// Get a match object, this contains members with team ids
		/// </summary>
		public void GetMatch(string matchId, Action<GetMatchResult> callback);

		/// <summary>
		/// Joins matchmaking queue
		/// </summary>
		public void JoinMatchmaking(MatchRoomSetup setup);

		/// <summary>
		/// Invokes that a game was found
		/// </summary>
		public void InvokeMatchFound(GameMatched match);

		public delegate void OnGameMatchedEventHandler(GameMatched match);

		public event OnGameMatchedEventHandler OnGameMatched;

		public delegate void OnMatchmakingJoinedHandler(JoinedMatchmaking match);

		public event OnMatchmakingJoinedHandler OnMatchmakingJoined;
	}


	/// <summary>
	/// Represents a match join settings
	/// This object is passed down through the network to all party players so please be careful
	/// when adding data here
	/// </summary>
	[Serializable]
	public class MatchRoomSetup
	{
		// Required at creation
		public int MapId;
		public int GameModeHash;
		public MatchType MatchType;
		public IReadOnlyList<string> Mutators;

		[CanBeNull] public string RoomIdentifier;
	}

	public class GameMatched
	{
		public string MatchIdentifier;
		public string TeamId;
		public MatchRoomSetup RoomSetup;
	}

	public class JoinedMatchmaking
	{
		public string TicketId;
		public MatchRoomSetup RoomSetup;
	}

	/// <inheritdoc cref="IMatchmakingService"/>
	public class PlayfabMatchmakingService : IMatchmakingService
	{
		private static string QUEUE_NAME = "flgranked"; // TODO: Drive from outside for multiple q 
		private const string LOBBY_TICKET_PROPERTY = "mm_match"; // TODO: Drive from outside for multiple q 
		private const string PLAYER_ENTITY_TYPE = "title_player_account";
		private IGameBackendService _gameBackend;
		private ICoroutineService _coroutines;
		private IPartyService _party;
		private MatchmakingPooling _pooling;

		public PlayfabMatchmakingService(IGameBackendService gameBackend, ICoroutineService coroutines, IPartyService party)
		{
			_gameBackend = gameBackend;
			_coroutines = coroutines;
			_party = party;
			_party.LobbyProperties.Observe(LOBBY_TICKET_PROPERTY, OnLobbyPropertyUpdate);
		}

		private void OnLobbyPropertyUpdate(string key, string before, string after, ObservableUpdateType arg4)
		{
			if (after == null)
			{
				if (_pooling != null)
				{
					_pooling.Stop();
				}

				return;
			}

			var model = ModelSerializer.Deserialize<JoinedMatchmaking>(after);
			var local = _party.Members.First(m => m.Local);
			if (local.Leader)
			{
				StartPolling(model);
				InvokeJoinedMatchmaking(model);
				return;
			}


			var req = new JoinMatchmakingTicketRequest()
			{
				QueueName = QUEUE_NAME,
				TicketId = model.TicketId,
				Member = new MatchmakingPlayer()
				{
					Entity = new EntityKey()
					{
						Id = local.PlayfabID,
						Type = PLAYER_ENTITY_TYPE
					}
				}
			};
			PlayFabMultiplayerAPI.JoinMatchmakingTicket(req, result =>
			{
				StartPolling(model);
				InvokeJoinedMatchmaking(model);
			}, Debug.LogError);
		}

		private void StartPolling(JoinedMatchmaking mm)
		{
			if (_pooling != null)
			{
				_pooling.Stop();
			}

			_pooling = new MatchmakingPooling(mm.TicketId, mm.RoomSetup, this, _coroutines);
			_pooling.Start();
		}


		public void LeaveMatchmaking()
		{
			PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
			{
				QueueName = QUEUE_NAME
			}, null, null);
			FLog.Verbose("Left Matchmaking");
		}

		public void GetTicket(string ticket, Action<GetMatchmakingTicketResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest()
			{
				QueueName = QUEUE_NAME,
				TicketId = ticket
			}, callback, Debug.LogError);
		}

		public void GetMyTickets(Action<ListMatchmakingTicketsForPlayerResult> callback)
		{
			PlayFabMultiplayerAPI.ListMatchmakingTicketsForPlayer(new ListMatchmakingTicketsForPlayerRequest()
			{
				QueueName = QUEUE_NAME
			}, callback, Debug.LogError);
		}

		public void GetMatch(string matchId, Action<GetMatchResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatch(new GetMatchRequest()
			{
				ReturnMemberAttributes = false,
				MatchId = matchId,
				QueueName = QUEUE_NAME
			}, callback, Debug.LogError);
		}


		public void JoinMatchmaking(MatchRoomSetup setup)
		{
			List<EntityKey> members = null;
			if (_party.HasParty.Value)
			{
				members = _party.Members.Where(pm => !pm.Leader).Select(pm => new EntityKey() {Id = pm.PlayfabID, Type = PLAYER_ENTITY_TYPE}).ToList();
			}

			PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
			{
				MembersToMatchWith = members, // HERE IS WHERE WE ADD THE SQUAD !!!
				QueueName = QUEUE_NAME,
				GiveUpAfterSeconds = 1000,
				Creator = new MatchmakingPlayer()
				{
					Entity = new EntityKey()
					{
						Id = PlayFabSettings.staticPlayer.EntityId,
						Type = PlayFabSettings.staticPlayer.EntityType
					}
				}
			}, r =>
			{
				var mm = new JoinedMatchmaking()
				{
					TicketId = r.TicketId,
					RoomSetup = setup
				};
				if (_party.HasParty.Value)
				{
					// If it is party the matchmaking transition will be handled by the OnLobbyPropertyChanges
					_party.SetLobbyProperty(LOBBY_TICKET_PROPERTY, ModelSerializer.Serialize(mm).Value);
					return;
				}

				StartPolling(mm);
				InvokeJoinedMatchmaking(mm);
			}, Debug.LogError);
		}

		public void InvokeMatchFound(GameMatched match)
		{
			match.RoomSetup.RoomIdentifier = match.MatchIdentifier;
			OnGameMatched?.Invoke(match);
		}

		private void InvokeJoinedMatchmaking(JoinedMatchmaking mm)
		{
			OnMatchmakingJoined?.Invoke(mm);
		}

		public event IMatchmakingService.OnGameMatchedEventHandler OnGameMatched;
		public event IMatchmakingService.OnMatchmakingJoinedHandler OnMatchmakingJoined;
	}

	/// <summary>
	/// Basic matchmaking pooling to check whenever our match is ready.
	/// SHould be replaced with websockets notification soon
	/// </summary>
	public class MatchmakingPooling
	{
		private string _ticket;
		private MatchRoomSetup _setup;
		private IMatchmakingService _service;
		private ICoroutineService _routines;
		private Coroutine _task;
		private bool _pooling = false;

		public MatchmakingPooling(string ticket, MatchRoomSetup setup, IMatchmakingService service, ICoroutineService coroutines)
		{
			_ticket = ticket;
			_service = service;
			_routines = coroutines;
			_setup = setup;
		}

		public void Start()
		{
			_task = _routines.StartCoroutine(Runnable());
		}

		public void Stop()
		{
			_routines.StopCoroutine(_task);
		}

		private IEnumerator Runnable()
		{
			var delay = new WaitForSeconds(7);
			_pooling = true;
			while (_pooling)
			{
				_service.GetTicket(_ticket, ticket =>
				{
					Debug.Log("Ticket Pool: " + JsonConvert.SerializeObject(ticket));
					// TODO: Check when ticket expired and expose event
					if (ticket.Status == "Matched")
					{
						// lets ride this callback hell YEEEEEEEEHAAAAAAAAA
						_service.GetMatch(ticket.MatchId, result =>
						{
							// Distribute teams
							var membersWithTeam = result.Members
								.ToDictionary(player => player.Entity.Id,
									player => player.TeamId
								);

							// This distribution should be deterministic and used in the server to validate if anyone is exploiting
							membersWithTeam = TeamDistribution.Distribute(membersWithTeam, _setup.GameMode().MaxPlayersInTeam);

							_service.InvokeMatchFound(new GameMatched()
							{
								MatchIdentifier = ticket.MatchId,
								RoomSetup = _setup,
								TeamId = membersWithTeam[PlayFabSettings.staticPlayer.EntityId]
							});
						});

						_pooling = false;
					}
				});
				yield return delay;
			}
		}

		// TODO - ADD PLAYFAB ERROR HANDLING IDENTIAL TO THE ONE IN GAME BACKEND NETWORK SERVICE
	}
}