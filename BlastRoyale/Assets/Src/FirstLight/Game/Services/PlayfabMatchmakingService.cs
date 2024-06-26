using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Services;
using PlayFab;
using PlayFab.MultiplayerModels;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using PlayFab.Json;
using Quantum;
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
		public IObservableFieldReader<bool> IsMatchmaking { get; }

		/// <summary>
		/// Leaves all matchmaking tickets that are being waited
		/// </summary>
		public void LeaveMatchmaking();

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

	public class GameMatched
	{
		public string MatchIdentifier;
		public string[] ExpectedPlayers;
		public MatchRoomSetup RoomSetup;
		public PlayerJoinRoomProperties PlayerProperties;
	}

	class CustomMatchmakingPlayerProperties
	{
		public string MasterPlayerId;
		public string Server;
		public int PlayerCount;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string Map;

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
		private const string LOBBY_TICKET_PROPERTY = "mm_match";
		private const string CANCELLED_KEY = "cancelled";

		private readonly IGameDataProvider _dataProvider;
		private readonly ICoroutineService _coroutines;
		private readonly IPartyService _party;
		private readonly IGameNetworkService _networkService;
		private readonly IGameBackendService _backendService;
		private readonly LocalPrefsService _localPrefsService;
		internal readonly IConfigsProvider _configsProvider;
		private readonly IDataService _localMatchmakingData;
		private MatchmakingData _localData;
		private MatchmakingPooling _pooling;
		private ObservableField<bool> _isMatchmaking;

		public IObservableFieldReader<bool> IsMatchmaking => _isMatchmaking;

		public event IMatchmakingService.OnGameMatchedEventHandler OnGameMatched;
		public event IMatchmakingService.OnMatchmakingJoinedHandler OnMatchmakingJoined;
		public event IMatchmakingService.OnMatchmakingCancelledHandler OnMatchmakingCancelled;

		public PlayfabMatchmakingService(IGameDataProvider dataProviderProvider, ICoroutineService coroutines,
										 IPartyService party, IMessageBrokerService broker, IGameNetworkService networkService,
										 IGameBackendService backendService, IConfigsProvider configsProvider, LocalPrefsService localPrefsService)
		{
			_networkService = networkService;
			_dataProvider = dataProviderProvider;
			_backendService = backendService;
			_configsProvider = configsProvider;
			_coroutines = coroutines;
			_party = party;
			_isMatchmaking = new ObservableField<bool>(false);
			_localPrefsService = localPrefsService;

			_localMatchmakingData = new DataService();
			_localData = _localMatchmakingData.LoadData<MatchmakingData>();
			_party.LobbyProperties.Observe(LOBBY_TICKET_PROPERTY, OnLobbyTicketPropertyUpdated);
			_party.Members.Observe((i, before, after, type) =>
			{
				if (type is ObservableUpdateType.Added or ObservableUpdateType.Removed)
				{
					FLog.Info("StoppingMatchmaking because player added/removed from party!");
					StopMatchmaking();
				}

				if (type == ObservableUpdateType.Updated)
				{
					// lets check if an player cancelled the ticket
					if (after.RawProperties.TryGetValue(CANCELLED_KEY, out var cancelledValue))
					{
						FLog.Info("Received matchmaking cancellation from " + after.DisplayName);

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
			FLog.Info("StopMatchmaking invoked");
			LeaveMatchmaking();
		}

		private void OnAuthentication(SuccessAuthentication _)
		{
			LeaveMatchmaking();
		}

		private void OnLobbyTicketPropertyUpdated(string key, string before, string after, ObservableUpdateType arg4)
		{
			if (after == null)
			{
				// This case of canceling the ticket is handled at set member property
				return;
			}

			var model = ModelSerializer.Deserialize<JoinedMatchmaking>(after);
			var local = _party.Members.First(m => m.Local);
			if (local.Leader)
			{
				FLog.Info($"Started polling ticket {model.TicketId} because leader of the squad");
				StartPolling(model);
				InvokeJoinedMatchmaking(model);
				return;
			}

			var req = new JoinMatchmakingTicketRequest()
			{
				QueueName = model.RoomSetup.PlayfabQueue.QueueName,
				TicketId = model.TicketId,
				Member = CreateLocalMatchmakingPlayer(model.RoomSetup)
			};
			PlayFabMultiplayerAPI.JoinMatchmakingTicket(req, result =>
			{
				FLog.Info($"Joined matchmaking ticket {model.TicketId} from party and start polling");
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
			FLog.Info("Started polling");
		}

		public void CancelLocalMatchmaking()
		{
			if (_pooling != null)
			{
				_pooling.Stop();
				_pooling = null;
			}

			FLog.Info($"OnMatchmakingCancelled invoked");
			OnMatchmakingCancelled?.Invoke();
			_isMatchmaking.Value = false;
		}

		public void LeaveMatchmaking()
		{
			if (string.IsNullOrEmpty(_localData.LastQueue)) return;
			PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
			{
				QueueName = _localData.LastQueue
			}, null, ErrorCallback("CancellAllTickets"));
			FLog.Info("Left Matchmaking");
			if (_pooling != null)
			{
				if (_party.HasParty.Value)
				{
					// For party everything is handled at the OnMemberUpdated
					_party.SetMemberProperty(CANCELLED_KEY, _pooling.Ticket).Forget();
					return;
				}

				CancelLocalMatchmaking();
			}
		}

		public void GetTicket(string ticket, string queue, Action<GetMatchmakingTicketResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest()
			{
				QueueName = queue,
				TicketId = ticket
			}, callback, ErrorCallback("GetTicket"));
		}

		public void GetMatch(string matchId, string queue, Action<GetMatchResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatch(new GetMatchRequest()
			{
				ReturnMemberAttributes = true,
				MatchId = matchId,
				QueueName = queue,
			}, callback, ErrorCallback("GetMatch"));
		}

		private MatchmakingPlayer CreateLocalMatchmakingPlayer(MatchRoomSetup roomSetup)
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
					Server = _localPrefsService.ServerRegion.Value,
					// We need to send the map as null so it can be matched with everyone else
					Map = roomSetup.SimulationConfig.MapId != (int) GameId.Any ? roomSetup.SimulationConfig.MapId.ToString() : null,
					MasterPlayerId = _networkService.UserId,
					PlayerCount = 1
				}.Encode()
			};

			FLog.Info($"Created local matchmaking player {ModelSerializer.Serialize(mp).Value}!");
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

			FLog.Info($"Creating matchmaking ticket with {members?.Count} members!");
			PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
			{
				MembersToMatchWith = members,
				QueueName = setup.PlayfabQueue.QueueName,
				GiveUpAfterSeconds = setup.PlayfabQueue.TimeoutTimeInSeconds,
				Creator = CreateLocalMatchmakingPlayer(setup)
			}, r =>
			{
				FLog.Info($"Matchmaking ticket {r.TicketId} created!");

				var mm = new JoinedMatchmaking()
				{
					TicketId = r.TicketId,
					RoomSetup = setup
				};
				if (_party.HasParty.Value)
				{
					// If it is party the matchmaking transition will be handled by the OnLobbyPropertyChanges
					var serializedJoined = ModelSerializer.Serialize(mm).Value;
					_party.SetLobbyProperty(LOBBY_TICKET_PROPERTY, serializedJoined, true).Forget();
					FLog.Info($"Set lobby ticket property {serializedJoined} created!");
					return;
				}

				FLog.Info("Started polling after creating ticket because not member of party!");
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
					FLog.Info("Removing ticket from lobby properties because match was found!");
					_party.DeleteLobbyProperty(LOBBY_TICKET_PROPERTY).Forget();
				}
			}
		}

		private void InvokeJoinedMatchmaking(JoinedMatchmaking mm)
		{
			_localData.LastQueue = mm.RoomSetup.PlayfabQueue.QueueName;
			_localMatchmakingData.SaveData<MatchmakingData>();
			OnMatchmakingJoined?.Invoke(mm);
			_isMatchmaking.Value = true;
			FLog.Info("OnMatchmakingJoined invoked");
		}

		private Action<PlayFabError> ErrorCallback(string operation)
		{
			return err =>
			{
				FLog.Warn("Recoverable exception happened at " + operation);
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
			FLog.Info("HandlingTicketCancellation Reason:" + ticket.CancellationReasonString + " Ticket:" + ticket.TicketId);
			if (ticket.CancellationReasonString == "Timeout")
			{
				string matchId = "timeout-match-" + ticket.TicketId;
				FLog.Info("Ticket timed out, creating ticket only match " + matchId);
				var players = ticket.Members
					.Select(m => CustomMatchmakingPlayerProperties.Decode(m.Attributes).MasterPlayerId)
					.ToArray();

				var colorIndex = (byte) ticket.Members.Select(m => m.Entity.Id).OrderBy(a => a)
					.ToList()
					.IndexOf(PlayFabSettings.staticPlayer.EntityId);

				_service.InvokeMatchFound(new GameMatched()
				{
					ExpectedPlayers = players,
					MatchIdentifier = matchId,
					RoomSetup = _setup,
					PlayerProperties = new PlayerJoinRoomProperties()
					{
						// Since this game is only going to be this ticket, all the players should be in the same team
						Team = "team1",
						TeamColor = colorIndex
					}
				});
				return;
			}

			_service.CancelLocalMatchmaking();
		}

		private void HandleMatched(GetMatchmakingTicketResult ticket)
		{
			_service.GetMatch(ticket.MatchId, ticket.QueueName, result =>
			{
				FLog.Info($"Found match {ModelSerializer.Serialize(result).Value}");
				// Distribute teams
				var membersWithTeam = result.Members
					.ToDictionary(player => player.Entity.Id,
						player => player.TeamId
					);

				// This distribution should be deterministic and used in the server to validate if anyone is exploiting
				membersWithTeam = TeamDistribution.Distribute(membersWithTeam, (uint) _setup.SimulationConfig.TeamSize);
				var playerTeam = membersWithTeam[PlayFabSettings.staticPlayer.EntityId];

				var colorIndex = (byte) membersWithTeam.Where((kv) => kv.Value == playerTeam).Select(kv => kv.Key)
					.OrderBy(a => a)
					.ToList()
					.IndexOf(PlayFabSettings.staticPlayer.EntityId);

				var decodedPlayers = result.Members
					.Select(m => CustomMatchmakingPlayerProperties.Decode(m.Attributes))
					.ToArray();

				// Select map
				var map = decodedPlayers
					.Select(m => m.Map).Distinct()
					.FirstOrDefault(id => id != ((int) GameId.Any).ToString()) ?? ((int) GameId.Any).ToString();

				_setup.SimulationConfig.MapId = int.Parse(map);
				_service.InvokeMatchFound(new GameMatched()
				{
					ExpectedPlayers = decodedPlayers
						.Select(m => m.MasterPlayerId)
						.ToArray(),
					MatchIdentifier = ticket.MatchId,
					RoomSetup = _setup,
					PlayerProperties = new PlayerJoinRoomProperties()
					{
						// Since this game is only going to be this ticket, all the players should be in the same team
						Team = playerTeam,
						TeamColor = colorIndex
					},
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
				_service.GetTicket(Ticket, _setup.PlayfabQueue.QueueName, ticket =>
				{
					FLog.Info($"Ticket pooling {ModelSerializer.Serialize(ticket).Value}");
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
						FLog.Info($"Unhandled ticket status {ticket.Status}");
					}
				});
				yield return waitDelay;
				// If playfab timeout doesn't work, so the player won't get stuck in the matchmaking screen
				waiting += delay;
				FLog.Info($"Already waited {waiting}s for matchmaking!");
				var maxWait = _setup.PlayfabQueue.TimeoutTimeInSeconds + 15;
				if (waiting >= maxWait)
				{
					FLog.Info($"Canceling ticket because it take longer then {maxWait} seconds!");
					_service.CancelLocalMatchmaking();
				}
			}
		}

		// TODO - ADD PLAYFAB ERROR HANDLING IDENTIAL TO THE ONE IN GAME BACKEND NETWORK SERVICE
	}
}