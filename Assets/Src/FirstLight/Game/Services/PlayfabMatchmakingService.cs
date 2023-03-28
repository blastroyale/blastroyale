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
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Party;
using FirstLight.Game.StateMachines;
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
		public string RoomIdentifier = "";

		public override string ToString() => ModelSerializer.Serialize(this).Value;
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
		public string Server;

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
		private static string QUEUE_NAME = "flgtrios"; // TODO: Drive from outside for multiple q 
		private const string LOBBY_TICKET_PROPERTY = "mm_match";
		private const string CANCELLED_KEY = "cancelled";
		public const string LOG_TAG = "Matchmaking";
		public static int TICKET_TIMEOUT_SECONDS = 45;


		private readonly IGameDataProvider _dataProvider;
		private readonly ICoroutineService _coroutines;
		private readonly IPartyService _party;
		private readonly IGameNetworkService _networkService;
		private readonly IGameBackendService _backendService;

		private MatchmakingPooling _pooling;
		private ObservableField<bool> _isMatchmaking;

		public IObservableFieldReader<bool> IsMatchmaking => _isMatchmaking;

		public event IMatchmakingService.OnGameMatchedEventHandler OnGameMatched;
		public event IMatchmakingService.OnMatchmakingJoinedHandler OnMatchmakingJoined;
		public event IMatchmakingService.OnMatchmakingCancelledHandler OnMatchmakingCancelled;

		public PlayfabMatchmakingService(IGameDataProvider dataProviderProvider, ICoroutineService coroutines,
										 IPartyService party, IMessageBrokerService broker, IGameNetworkService networkService,
										 IGameBackendService backendService)
		{
			_networkService = networkService;
			_dataProvider = dataProviderProvider;
			_backendService = backendService;
			_coroutines = coroutines;
			_party = party;
			_isMatchmaking = new ObservableField<bool>(false);

			_party.LobbyProperties.Observe(LOBBY_TICKET_PROPERTY, OnLobbyPropertyUpdate);
			_party.Members.Observe((i, before, after, type) =>
			{
				if (type is ObservableUpdateType.Added or ObservableUpdateType.Removed)
				{
					FLog.Info(LOG_TAG, "StoppingMatchmaking because player added/removed from party!");
					StopMatchmaking();
				}

				if (type == ObservableUpdateType.Updated)
				{
					// lets check if an player cancelled the ticket
					if (after.RawProperties.TryGetValue(CANCELLED_KEY, out var cancelledValue))
					{
						FLog.Info(LOG_TAG, "Received matchmaking cancellation from " + after.DisplayName);

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
			FLog.Info(LOG_TAG, "StopMatchmaking invoked");

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
					FLog.Info(LOG_TAG, "Stopped pooling received null ticket value from squad");
					_pooling.Stop();
					_pooling = null;
				}

				return;
			}

			var model = ModelSerializer.Deserialize<JoinedMatchmaking>(after);
			var local = _party.Members.First(m => m.Local);
			if (local.Leader)
			{
				FLog.Info(LOG_TAG, $"Started polling ticket {model.TicketId} because leader of the squad");
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
				FLog.Info(LOG_TAG, $"Joined matchmaking ticket {model.TicketId} from party and start polling");
				StartPolling(model);
				InvokeJoinedMatchmaking(model);
			}, ErrorCallback("JoinMatchmakingTicket"));
		}

		private void StartPolling(JoinedMatchmaking mm)
		{
			if (_pooling != null)
			{
				_pooling.Stop();
			}

			_pooling = new MatchmakingPooling(mm.TicketId, mm.RoomSetup, this, _coroutines);
			_pooling.Start();
			FLog.Info(LOG_TAG, "Started polling");
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

			FLog.Info(LOG_TAG, $"OnMatchmakingCancelled invoked");
			OnMatchmakingCancelled?.Invoke();
			_isMatchmaking.Value = false;
		}

		public void LeaveMatchmaking()
		{
			PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
			{
				QueueName = QUEUE_NAME
			}, null, ErrorCallback("CancellAllTickets"));
			FLog.Info(LOG_TAG, "Left Matchmaking");
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
			}, callback, ErrorCallback("GetTicket"));
		}


		public void GetMyTickets(Action<ListMatchmakingTicketsForPlayerResult> callback)
		{
			PlayFabMultiplayerAPI.ListMatchmakingTicketsForPlayer(new ListMatchmakingTicketsForPlayerRequest()
			{
				QueueName = QUEUE_NAME
			}, callback, ErrorCallback("GetMyTickets"));
		}

		public void GetMatch(string matchId, Action<GetMatchResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatch(new GetMatchRequest()
			{
				ReturnMemberAttributes = true,
				MatchId = matchId,
				QueueName = QUEUE_NAME
			}, callback, ErrorCallback("GetMatch"));
		}

		private MatchmakingPlayer CreateLocalMatchmakingPlayer()
		{
			var mp = new MatchmakingPlayer()
			{
				Entity = new EntityKey()
				{
					Id = PlayFabSettings.staticPlayer.EntityId,
					Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE,
				},
				Attributes = new CustomMatchmakingPlayerProperties()
				{
					Server = _dataProvider.AppDataProvider.ConnectionRegion.Value,
					MasterPlayerId = _networkService.UserId
				}.Encode()
			};

			FLog.Info(LOG_TAG, $"Created local matchmaking player {ModelSerializer.Serialize(mp).Value}!");
			return mp;
		}

		public void JoinMatchmaking(MatchRoomSetup setup)
		{
			List<EntityKey> members = null;
			if (_party.HasParty.Value)
			{
				members = _party.Members.Where(pm => !pm.Leader)
					.Select(pm => pm.ToEntityKey()).ToList();
			}

			FLog.Info(LOG_TAG, $"Creating matchmaking ticket with {members?.Count} members!");
			PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
			{
				MembersToMatchWith = members,
				QueueName = QUEUE_NAME,
				GiveUpAfterSeconds = TICKET_TIMEOUT_SECONDS,
				Creator = CreateLocalMatchmakingPlayer()
			}, r =>
			{
				FLog.Info(LOG_TAG, $"Matchmaking ticket {r.TicketId} created!");

				var mm = new JoinedMatchmaking()
				{
					TicketId = r.TicketId,
					RoomSetup = setup
				};
				if (_party.HasParty.Value)
				{
					// If it is party the matchmaking transition will be handled by the OnLobbyPropertyChanges
					var serializedJoined = ModelSerializer.Serialize(mm).Value;
					_party.SetLobbyProperty(LOBBY_TICKET_PROPERTY, serializedJoined);
					FLog.Info(LOG_TAG, $"Set lobby ticket property {serializedJoined} created!");
					return;
				}

				FLog.Info(LOG_TAG, "Started polling after creating ticket because not member of party!");
				StartPolling(mm);
				InvokeJoinedMatchmaking(mm);
			}, ErrorCallback("CreateMatchmakingTicket"));
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
					FLog.Info(LOG_TAG, "Removing ticket from lobby properties because match was found!");
					_party.DeleteLobbyProperty(LOBBY_TICKET_PROPERTY);
				}
				else
				{
					FLog.Info(LOG_TAG, "Setting ready to false because match was found");
					_party.Ready(false);
				}
			}
		}

		private void InvokeJoinedMatchmaking(JoinedMatchmaking mm)
		{
			OnMatchmakingJoined?.Invoke(mm);
			_isMatchmaking.Value = true;
			FLog.Info(LOG_TAG, "OnMatchmakingJoined invoked");
		}


		private Action<PlayFabError> ErrorCallback(string operation)
		{
			return err =>
			{
				FLog.Warn(LOG_TAG, "Recoverable exception happened at " + operation);
				var ex = err.AsException();
				_backendService.HandleRecoverableException(ex);
			};
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
			FLog.Info(PlayfabMatchmakingService.LOG_TAG, "HandlingTicketCancellation Reason:" + ticket.CancellationReasonString + " Ticket:" + ticket.TicketId);
			if (ticket.CancellationReasonString == "Timeout")
			{
				string matchId = "timeout-match-" + ticket.TicketId;
				FLog.Info(PlayfabMatchmakingService.LOG_TAG, "Ticket timed out, creating ticket only match " + matchId);
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
				FLog.Info(PlayfabMatchmakingService.LOG_TAG, $"Found match {ModelSerializer.Serialize(result).Value}");
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
			float delay = 6.5f;
			var waitDelay = new WaitForSeconds(delay);
			float waiting = 0;
			_pooling = true;
			while (_pooling)
			{
				_service.GetTicket(Ticket, ticket =>
				{
					FLog.Info(PlayfabMatchmakingService.LOG_TAG, $"Ticket pooling {ModelSerializer.Serialize(ticket).Value}");
					// TODO: Check when ticket expired and expose event
					if (ticket.Status == "Matched")
					{
						HandleMatched(ticket);
						_pooling = false;
					}
					else if (ticket.Status == "Canceled")
					{
						HandleCancellation(ticket);
						_pooling = false;
					}
					else
					{
						FLog.Info(PlayfabMatchmakingService.LOG_TAG, $"Unhandled ticket status {ticket.Status}");
					}
				});
				yield return waitDelay;
				// If playfab timeout doesn't work, so the player won't get stuck in the matchmaking screen
				waiting += delay;
				FLog.Info(PlayfabMatchmakingService.LOG_TAG, $"Already waited {waiting}s for matchmaking!");
				var maxWait = PlayfabMatchmakingService.TICKET_TIMEOUT_SECONDS + 30;
				if (waiting >= maxWait)
				{
					FLog.Info(PlayfabMatchmakingService.LOG_TAG, $"Canceling ticket because it take longer then {maxWait} seconds!");
					_service.CancelLocalMatchmaking();
				}
			}
		}

		// TODO - ADD PLAYFAB ERROR HANDLING IDENTIAL TO THE ONE IN GAME BACKEND NETWORK SERVICE
	}
}