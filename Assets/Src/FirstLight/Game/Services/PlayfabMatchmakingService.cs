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
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
using PlayFab.Json;
using SRF;
using UnityEngine;
using UnityEngine.Serialization;

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
		public IObservableFieldReader<bool> IsMatchmaking { get; }

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

		/// <summary>
		/// Event dispatcher when a game is found by Matchmaking
		/// </summary>
		public event OnGameMatchedEventHandler OnGameMatched;

		public delegate void OnMatchmakingJoinedHandler(JoinedMatchmaking match);

		/// <summary>
		/// Event triggered when a player enter matchmaking, triggered for all party members
		/// </summary>
		public event OnMatchmakingJoinedHandler OnMatchmakingJoined;

		public delegate void OnMatchmakingCancelledHandler();

		/// <summary>
		/// Dispatched when the matchmaking got canceled, either by timeout or when a player manually cancel it
		/// </summary>
		public event OnMatchmakingCancelledHandler OnMatchmakingCancelled;
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
		public string GameModeId;
		public MatchType MatchType;
		public IReadOnlyList<string> Mutators;

		[CanBeNull] public string RoomIdentifier;
	}

	public class GameMatched
	{
		public string MatchIdentifier;
		public string TeamId;
		public string[] ExpectedPlayers;
		public MatchRoomSetup RoomSetup;
	}

	class CustomMatchmakingPlayerProperties
	{
		public string MasterPlayerId;

		public MatchmakingPlayerAttributes Encode()
		{
			return new MatchmakingPlayerAttributes()
			{
				EscapedDataObject = PlayFabSimpleJson.SerializeObject(this)
			};
		}

		public static CustomMatchmakingPlayerProperties Decode(MatchmakingPlayerAttributes attributes)
		{
			return PlayFabSimpleJson.DeserializeObject<CustomMatchmakingPlayerProperties>(attributes.DataObject.ToString());
		}
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
		private const string LOBBY_TICKET_PROPERTY = "mm_match";
		private const string CANCELLED_KEY = "cancelled";
		private IGameBackendService _gameBackend;
		private ICoroutineService _coroutines;
		private IPartyService _party;
		private MatchmakingPooling _pooling;
		private ObservableField<bool> _isMatchmaking;

		public IObservableFieldReader<bool> IsMatchmaking => _isMatchmaking;

		public event IMatchmakingService.OnGameMatchedEventHandler OnGameMatched;
		public event IMatchmakingService.OnMatchmakingJoinedHandler OnMatchmakingJoined;
		public event IMatchmakingService.OnMatchmakingCancelledHandler OnMatchmakingCancelled;

		public PlayfabMatchmakingService(IGameBackendService gameBackend, ICoroutineService coroutines,
										 IPartyService party, IMessageBrokerService broker)
		{
			_gameBackend = gameBackend;
			_coroutines = coroutines;
			_party = party;
			_isMatchmaking = new ObservableField<bool>(false);

			_party.LobbyProperties.Observe(LOBBY_TICKET_PROPERTY, OnLobbyPropertyUpdate);
			_party.Members.Observe((i, before, after, type) =>
			{
				if (type is ObservableUpdateType.Added or ObservableUpdateType.Removed)
				{
					StopMatchmaking();
				}

				if (type == ObservableUpdateType.Updated)
				{
					// lets check if an player cancelled the ticket
					if (after.RawProperties.TryGetValue(CANCELLED_KEY, out var cancelledValue))
					{
						if (_pooling != null)
						{
							if (_pooling.Ticket == cancelledValue)
							{
								CancelLocalMatchmaking();
							}
						}
					}
				}
			});
			broker.Subscribe<SuccessAuthentication>(OnAuthentication);
		}

		private void StopMatchmaking()
		{
			if (_pooling != null)
			{
				LeaveMatchmaking();
				_pooling.Stop();
				_pooling = null;
			}
		}

		private void OnAuthentication(SuccessAuthentication _)
		{
			LeaveMatchmaking();
		}

		private void OnLobbyPropertyUpdate(string key, string before, string after, ObservableUpdateType arg4)
		{
			if (after == null)
			{
				if (_pooling != null)
				{
					_pooling.Stop();
					_pooling = null;
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
				Member = CreateLocalMatchmakingPlayer()
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

		public void CancelLocalMatchmaking()
		{
			if (_party.HasParty.Value)
			{
				_party.Ready(false);
			}

			if (_pooling != null)
			{
				_pooling.Stop();
				_pooling = null;
			}

			OnMatchmakingCancelled?.Invoke();
			_isMatchmaking.Value = false;
		}

		public void LeaveMatchmaking()
		{
			PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
			{
				QueueName = QUEUE_NAME
			}, null, Debug.LogError);
			FLog.Verbose("Left Matchmaking");
			if (_pooling != null)
			{
				if (_party.HasParty.Value)
				{
					// For party everything is handled at the OnMemberUpdated
					_party.SetMemberProperty(CANCELLED_KEY, _pooling.Ticket);
					return;
				}

				CancelLocalMatchmaking();
			}
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
				ReturnMemberAttributes = true,
				MatchId = matchId,
				QueueName = QUEUE_NAME
			}, callback, Debug.LogError);
		}

		private MatchmakingPlayer CreateLocalMatchmakingPlayer()
		{
			return new MatchmakingPlayer()
			{
				Entity = new EntityKey()
				{
					Id = PlayFabSettings.staticPlayer.EntityId,
					Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE,
				},
				Attributes = new CustomMatchmakingPlayerProperties()
				{
					MasterPlayerId = PlayFabSettings.staticPlayer.PlayFabId
				}.Encode()
			};
		}

		public void JoinMatchmaking(MatchRoomSetup setup)
		{
			List<EntityKey> members = null;
			if (_party.HasParty.Value)
			{
				members = _party.Members.Where(pm => !pm.Leader)
					.Select(pm => pm.ToEntityKey()).ToList();
			}

			PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
			{
				MembersToMatchWith = members, // HERE IS WHERE WE ADD THE SQUAD !!!
				QueueName = QUEUE_NAME,
				GiveUpAfterSeconds = 45,
				Creator = CreateLocalMatchmakingPlayer()
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
			_isMatchmaking.Value = false;
			if (_party.HasParty.Value)
			{
				if (_party.GetLocalMember().Leader)
				{
					_party.DeleteLobbyProperty(LOBBY_TICKET_PROPERTY);
				}
				else
				{
					_party.Ready(false);
				}
			}
		}

		private void InvokeJoinedMatchmaking(JoinedMatchmaking mm)
		{
			OnMatchmakingJoined?.Invoke(mm);
			_isMatchmaking.Value = true;
		}
	}

	/// <summary>
	/// Basic matchmaking pooling to check whenever our match is ready.
	/// SHould be replaced with websockets notification soon
	/// </summary>
	public class MatchmakingPooling
	{
		public string Ticket { get; }
		private MatchRoomSetup _setup;
		private PlayfabMatchmakingService _service;
		private ICoroutineService _routines;
		private Coroutine _task;
		private bool _pooling = false;

		public MatchmakingPooling(string ticket, MatchRoomSetup setup, PlayfabMatchmakingService service,
								  ICoroutineService coroutines)
		{
			Ticket = ticket;
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

		private void HandleCancellation(GetMatchmakingTicketResult ticket)
		{
			if (ticket.CancellationReasonString == "Timeout")
			{
				string matchId = "timeout-match-" + ticket.TicketId;
				_service.InvokeMatchFound(new GameMatched()
				{
					ExpectedPlayers = ticket.Members
						.Select(m => CustomMatchmakingPlayerProperties.Decode(m.Attributes).MasterPlayerId)
						.ToArray(),
					MatchIdentifier = matchId,
					RoomSetup = _setup,
					// Since this game is only going to be this ticket, all the players should be in the same team
					TeamId = "team1"
				});
				return;
			}

			_service.CancelLocalMatchmaking();
		}

		private void HandleMatched(GetMatchmakingTicketResult ticket)
		{
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
					ExpectedPlayers = result.Members
						.Select(m => CustomMatchmakingPlayerProperties.Decode(m.Attributes).MasterPlayerId)
						.ToArray(),
					MatchIdentifier = ticket.MatchId,
					RoomSetup = _setup,
					TeamId = membersWithTeam[PlayFabSettings.staticPlayer.EntityId]
				});
			});
		}


		private IEnumerator Runnable()
		{
			var delay = new WaitForSeconds(6.5f);
			_pooling = true;
			while (_pooling)
			{
				_service.GetTicket(Ticket, ticket =>
				{
					Debug.Log("Ticket Pool: " + JsonConvert.SerializeObject(ticket));
					// TODO: Check when ticket expired and expose event
					if (ticket.Status == "Matched")
					{
						HandleMatched(ticket);
						_pooling = false;
					}

					if (ticket.Status == "Canceled")
					{
						HandleCancellation(ticket);
						_pooling = false;
					}
				});
				yield return delay;
			}
		}

		// TODO - ADD PLAYFAB ERROR HANDLING IDENTIAL TO THE ONE IN GAME BACKEND NETWORK SERVICE
	}
}