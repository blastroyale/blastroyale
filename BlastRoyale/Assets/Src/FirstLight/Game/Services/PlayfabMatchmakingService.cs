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
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Newtonsoft.Json;
using PlayFab.Json;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using NullValueHandling = PlayFab.Json.NullValueHandling;

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

		// Players will only be matched with others who have the same key 
		public string DistinctionKey;

		[PlayFab.Json.JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
		private BufferedQueue _requestBuffer = new (TimeSpan.FromSeconds(1));

		private readonly IGameDataProvider _dataProvider;

		private readonly ICoroutineService _coroutines;

		private readonly IFLLobbyService _lobbyService;
		private readonly IGameNetworkService _networkService;
		private readonly IGameBackendService _backendService;
		private readonly LocalPrefsService _localPrefsService;
		internal readonly IConfigsProvider _configsProvider;
		private readonly IDataService _localMatchmakingData;
		internal readonly IGameModeService _gameModeService;
		private MatchmakingData _localData;
		private MatchmakingPooling _pooling;
		private ObservableField<bool> _isMatchmaking;

		public IObservableFieldReader<bool> IsMatchmaking => _isMatchmaking;

		public event IMatchmakingService.OnGameMatchedEventHandler OnGameMatched;
		public event IMatchmakingService.OnMatchmakingJoinedHandler OnMatchmakingJoined;
		public event IMatchmakingService.OnMatchmakingCancelledHandler OnMatchmakingCancelled;

		public PlayfabMatchmakingService(IGameDataProvider dataProviderProvider, ICoroutineService coroutines, IFLLobbyService lobbyService,
										 IMessageBrokerService broker,
										 IGameNetworkService networkService,
										 IGameBackendService backendService, IConfigsProvider configsProvider, LocalPrefsService localPrefsService, IGameModeService gameModeService)
		{
			_networkService = networkService;
			_dataProvider = dataProviderProvider;
			_backendService = backendService;
			_configsProvider = configsProvider;
			_coroutines = coroutines;
			_lobbyService = lobbyService;
			_isMatchmaking = new ObservableField<bool>(false);
			_localPrefsService = localPrefsService;
			_gameModeService = gameModeService;

			_localMatchmakingData = new DataService();
			_localData = _localMatchmakingData.LoadData<MatchmakingData>();

			_lobbyService.CurrentPartyCallbacks.PlayerJoined += _ => StopMatchmaking();
			_lobbyService.CurrentPartyCallbacks.PlayerLeft += _ => StopMatchmaking();
			_lobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnPartyLobbyChanged;
			broker.Subscribe<SuccessAuthentication>(OnAuthentication);
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			if (changes == null || changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
			{
				StopMatchmaking();
				return;
			}

			if (changes.Data.Changed || changes.Data.Added || changes.Data.Removed)
			{
				if (changes.Data.Value.TryGetValue(FLLobbyService.KEY_MATCHMAKING_TICKET, out var ticketLobbyValue))
				{
					var ticket = ticketLobbyValue.Value.Value == null
						? null
						: JsonConvert.DeserializeObject<JoinedMatchmaking>(ticketLobbyValue.Value.Value);

					if (ticket != null)
					{
						OnPartyMatchmakingTicketReceived(ticket);
					}
					else
					{
						CancelLocalMatchmaking();
					}
				}
			}
		}

		private void StopMatchmaking()
		{
			FLog.Info("StopMatchmaking invoked");
			if (MainInstaller.ResolveServices().MatchmakingService.IsMatchmaking.Value)
			{
				LeaveMatchmaking();
			}
		}

		private void OnAuthentication(SuccessAuthentication _)
		{
			LeaveMatchmaking();
		}

		private void OnPartyMatchmakingTicketReceived(JoinedMatchmaking model)
		{
			if (_lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
			{
				FLog.Info($"Started polling ticket {model.TicketId} because leader of the squad");
				StartPolling(model);
				InvokeJoinedMatchmaking(model);
				return;
			}

			var queueName = _gameModeService.GetTeamSizeFor(model.RoomSetup.SimulationConfig).QueueName;
			var req = new JoinMatchmakingTicketRequest()
			{
				QueueName = queueName,
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
			_requestBuffer.Add(() =>
			{
				if (string.IsNullOrEmpty(_localData.LastQueue)) return;
				PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
				{
					QueueName = _localData.LastQueue
				}, null, ErrorCallback("CancellAllTickets"));
				FLog.Info("Left Matchmaking");
				if (_pooling != null)
				{
					if (_lobbyService.CurrentPartyLobby != null && _lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
					{
						_lobbyService.UpdatePartyMatchmakingTicket(null).Forget();
					}

					CancelLocalMatchmaking();
				}
			});
		}

		public void GetTicket(string ticket, string queue, Action<GetMatchmakingTicketResult> callback)
		{
			_requestBuffer.Add(() =>
			{
				PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest()
				{
					QueueName = queue,
					TicketId = ticket
				}, callback, ErrorCallback("GetTicket"));
			});
		}

		public void GetMatch(string matchId, string queue, Action<GetMatchResult> callback)
		{
			_requestBuffer.Add(() =>
			{
				PlayFabMultiplayerAPI.GetMatch(new GetMatchRequest()
				{
					ReturnMemberAttributes = true,
					MatchId = matchId,
					QueueName = queue,
				}, callback, ErrorCallback("GetMatch"));
			});
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
					Map = roomSetup.SimulationConfig.MapId != GameId.Any.ToString() ? roomSetup.SimulationConfig.MapId.ToString() : null,
					MasterPlayerId = _networkService.UserId,
					PlayerCount = 1,
					DistinctionKey = roomSetup.SimulationConfig.UniqueConfigId
				}.Encode()
			};

			FLog.Info($"Created local matchmaking player {ModelSerializer.Serialize(mp).Value}!");
			return mp;
		}

		public void JoinMatchmaking(MatchRoomSetup setup)
		{
			_requestBuffer.Add(() =>
			{
				List<EntityKey> members = null;
				var partyLobby = _lobbyService.CurrentPartyLobby;
				if (partyLobby != null)
				{
					members = partyLobby.Players.Where(p => !p.IsLocal())
						.Select(p => p.ToEntityKey()).ToList();
				}

				FLog.Info($"Creating matchmaking ticket with {members?.Count} members!");
				var queueConfig = _gameModeService.GetTeamSizeFor(setup.SimulationConfig);

				PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
				{
					MembersToMatchWith = members,
					QueueName = queueConfig.QueueName,
					GiveUpAfterSeconds = queueConfig.QueueTimeoutTimeInSeconds,
					Creator = CreateLocalMatchmakingPlayer(setup)
				}, r =>
				{
					FLog.Info($"Matchmaking ticket {r.TicketId} created!");

					var mm = new JoinedMatchmaking
					{
						TicketId = r.TicketId,
						RoomSetup = setup
					};
					if (partyLobby != null && partyLobby.IsLocalPlayerHost())
					{
						FLog.Info($"Set lobby ticket property {ModelSerializer.Serialize(mm).Value} created!");
						UpdateMatchmakingTicket(mm).Forget();
						return;
					}

					FLog.Info("Started polling after creating ticket because not member of party!");
					StartPolling(mm);
					InvokeJoinedMatchmaking(mm);
				}, ErrorCallback("CreateMatchmakingTicket"));
			});
		}

		private async UniTaskVoid UpdateMatchmakingTicket(JoinedMatchmaking mm)
		{
			var success = await _lobbyService.UpdatePartyMatchmakingTicket(mm);
			if (success)
			{
				OnPartyMatchmakingTicketReceived(mm);
			}
		}

		public void InvokeMatchFound(GameMatched match)
		{
			match.RoomSetup.RoomIdentifier = match.MatchIdentifier;
			OnGameMatched?.Invoke(match);
			_isMatchmaking.Value = false;
			var partyLobby = _lobbyService.CurrentPartyLobby;
			if (partyLobby != null && partyLobby.IsLocalPlayerHost())
			{
				FLog.Info("Removing ticket from lobby properties because match was found!");
				//_party.DeleteLobbyProperty(LOBBY_TICKET_PROPERTY).Forget();
				//_lobbyService.UpdatePartyMatchmakingTicket(null).Forget(); // TODO: Will likely break
			}
		}

		private void InvokeJoinedMatchmaking(JoinedMatchmaking mm)
		{
			var queueName = _gameModeService.GetTeamSizeFor(mm.RoomSetup.SimulationConfig).QueueName;
			_localData.LastQueue = queueName;
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
					.FirstOrDefault(id => id != GameId.Any.ToString()) ?? GameId.Any.ToString();

				_setup.SimulationConfig.MapId = map;
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
				yield return waitDelay;
				var queueConfig = _service._gameModeService.GetTeamSizeFor(_setup.SimulationConfig);
				_service.GetTicket(Ticket, queueConfig.QueueName, ticket =>
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
				// If playfab timeout doesn't work, so the player won't get stuck in the matchmaking screen
				waiting += delay;
				FLog.Info($"Already waited {waiting}s for matchmaking!");
				var maxWait = queueConfig.QueueTimeoutTimeInSeconds + 15;
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