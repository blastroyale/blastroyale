using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Services;
using PlayFab;
using PlayFab.MultiplayerModels;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
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
using I2.Loc;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PlayFab.Json;
using Quantum;
using Unity.Services.Lobbies;
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
		public UniTask LeaveMatchmaking();

		/// <summary>
		/// Joins matchmaking queue
		/// </summary>
		public UniTask JoinMatchmaking(MatchRoomSetup setup);
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

		[UsedImplicitly] public string Server;

		// Used for matchmaking algorithm, so we can decrease the min player amount over time
		[UsedImplicitly] public int PlayerCount;

		// Players will only be matched with others who have the same key 
		[UsedImplicitly] public string DistinctionKey;

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
		public string RoomSetupBase64;

		public MatchRoomSetup DeserializeRoomSetup()
		{
			var bytes = Convert.FromBase64String(RoomSetupBase64);
			if (!MatchRoomSetup.TryParseMatchRoomSetup(bytes, out var setup))
			{
				throw new Exception("Could not deserialize MatchRoomSetup");
			}

			return setup;
		}
	}

	/// <inheritdoc cref="IMatchmakingService"/>
	public class PlayfabMatchmakingService : IMatchmakingService
	{
		internal static readonly TimeSpan PoolingInterval = TimeSpan.FromSeconds(6);

		private readonly IFLLobbyService _lobbyService;
		private readonly IMessageBrokerService _broker;
		private readonly IGameNetworkService _networkService;
		private readonly IGameBackendService _backendService;
		private readonly LocalPrefsService _localPrefsService;
		private readonly IDataService _localMatchmakingData;
		internal readonly IGameModeService _gameModeService;
		private MatchmakingData _localData;
		private MatchmakingPooling _pooling;
		private ObservableField<bool> _isMatchmaking;
		internal readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);

		public IObservableFieldReader<bool> IsMatchmaking => _isMatchmaking;

		public PlayfabMatchmakingService(IGameDataProvider dataProviderProvider, ICoroutineService coroutines, IFLLobbyService lobbyService,
										 IMessageBrokerService broker,
										 IGameNetworkService networkService,
										 IGameBackendService backendService, IConfigsProvider configsProvider, LocalPrefsService localPrefsService,
										 IGameModeService gameModeService)
		{
			_networkService = networkService;
			_backendService = backendService;
			_lobbyService = lobbyService;
			_broker = broker;
			_isMatchmaking = new ObservableField<bool>(false);
			_localPrefsService = localPrefsService;
			_gameModeService = gameModeService;

			_localMatchmakingData = new DataService();
			_localData = _localMatchmakingData.LoadData<MatchmakingData>();

			_lobbyService.CurrentPartyCallbacks.PlayerJoined += _ => StopMatchmaking();
			_lobbyService.CurrentPartyCallbacks.PlayerLeft += _ => StopMatchmaking();
			_lobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnPartyLobbyChanged;
			broker.Subscribe<SuccessfullyAuthenticated>(OnAuthentication);
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
			if (IsMatchmaking.Value)
			{
				LeaveMatchmaking().Forget();
			}
		}

		private void OnAuthentication(SuccessfullyAuthenticated _)
		{
			LeaveMatchmaking().Forget();
		}

		private void OnPartyMatchmakingTicketReceived(JoinedMatchmaking model)
		{
			UniTask.Void(async () =>
			{
				using (await _mutex.AcquireAsync())
				{
					if (_lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
					{
						FLog.Info($"Started polling ticket {model.TicketId} because leader of the squad");
						StartPolling(model);
						InvokeJoinedMatchmaking(model);
						return;
					}

					var roomSetup = model.DeserializeRoomSetup();
					var queueName = _gameModeService.GetTeamSizeFor(roomSetup.SimulationConfig).QueueName;
					var req = new JoinMatchmakingTicketRequest()
					{
						QueueName = queueName,
						TicketId = model.TicketId,
						Member = CreateLocalMatchmakingPlayer(roomSetup)
					};

					var result = await AsyncPlayfabAPI.MultiplayerAPI.JoinMatchmakingTicket(req);
					FLog.Info($"Joined matchmaking ticket {model.TicketId} from party and start polling");
					StartPolling(model);
					InvokeJoinedMatchmaking(model);
				}
			});
		}

		private void StartPolling(JoinedMatchmaking mm)
		{
			if (_pooling != null)
			{
				_pooling.Stop();
			}

			_pooling = new MatchmakingPooling(mm.TicketId, mm.DeserializeRoomSetup(), this);
			_pooling.Start();
			FLog.Info("Started polling");
		}

		public void CancelLocalMatchmaking(bool error = false, string reason = null)
		{
			if (_pooling != null)
			{
				_pooling.Stop();
				_pooling = null;
			}

			_isMatchmaking.Value = false;
			FLog.Info($"OnMatchmakingCancelled invoked");
			_broker.Publish(new MatchmakingLeftMessage()
			{
				Error = error,
				Reason = reason
			});
		}

		public async UniTask LeaveMatchmaking()
		{
			using (await _mutex.AcquireAsync())
			{
				if (string.IsNullOrEmpty(_localData.LastQueue)) return;

				AsyncPlayfabAPI.MultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
				{
					QueueName = _localData.LastQueue,
				}).Forget();
				FLog.Info("Left Matchmaking");
				if (_pooling != null)
				{
					if (_lobbyService.CurrentPartyLobby != null && _lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
					{
						_lobbyService.UpdatePartyMatchmakingTicket(null).Forget();
					}

					CancelLocalMatchmaking();
				}
			}
		}

		public UniTask<GetMatchmakingTicketResult> GetTicket(string ticket, string queue)
		{
			return AsyncPlayfabAPI.MultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest()
			{
				QueueName = queue,
				TicketId = ticket
			});
		}

		public UniTask<GetMatchResult> GetMatch(string matchId, string queue)
		{
			return AsyncPlayfabAPI.MultiplayerAPI.GetMatch(new GetMatchRequest()
			{
				ReturnMemberAttributes = true,
				MatchId = matchId,
				QueueName = queue,
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

		public async UniTask JoinMatchmaking(MatchRoomSetup setup)
		{
			using (await _mutex.AcquireAsync())
			{
				List<EntityKey> members = null;
				var partyLobby = _lobbyService.CurrentPartyLobby;
				if (partyLobby != null)
				{
					members = partyLobby.Players.Where(p => !p.IsLocal())
						.Select(p => p.ToEntityKey()).ToList();
				}

				FLog.Info($"Creating matchmaking ticket with {members?.Count} members!");
				var queueConfig = _gameModeService.GetMatchMakingConfigFor(setup.SimulationConfig);

				var r = await AsyncPlayfabAPI.MultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
				{
					MembersToMatchWith = members,
					QueueName = queueConfig.QueueName,
					GiveUpAfterSeconds = queueConfig.QueueTimeoutTimeInSeconds,
					Creator = CreateLocalMatchmakingPlayer(setup)
				});

				FLog.Info($"Matchmaking ticket {r.TicketId} created!");

				var mm = new JoinedMatchmaking
				{
					TicketId = r.TicketId,
					RoomSetupBase64 = Convert.ToBase64String(setup.ToByteArray())
				};
				if (partyLobby != null && partyLobby.IsLocalPlayerHost())
				{
					FLog.Info($"Set lobby ticket property {ModelSerializer.Serialize(mm).Value} created!");
					await UpdateMatchmakingTicket(mm);
					return;
				}

				FLog.Info("Started polling after creating ticket because not member of party!");
				StartPolling(mm);
				InvokeJoinedMatchmaking(mm);
			}
		}

		private async UniTask UpdateMatchmakingTicket(JoinedMatchmaking mm)
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
			_broker.Publish(new MatchmakingMatchFoundMessage() {Game = match});

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
			var queueName = _gameModeService.GetMatchMakingConfigFor(mm.DeserializeRoomSetup().SimulationConfig).QueueName;
			_localData.LastQueue = queueName;
			_localData.TicketId = mm.TicketId;
			_localMatchmakingData.SaveData<MatchmakingData>();
			_broker.Publish(new MatchmakingJoinedMessage() {Config = mm});

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
		private CancellationTokenSource _cancellationToken;

		public MatchmakingPooling(string ticket, MatchRoomSetup setup, PlayfabMatchmakingService service)
		{
			Ticket = ticket;
			_service = service;
			_setup = setup;
			_cancellationToken = new CancellationTokenSource();
		}

		public void Start()
		{
			PoolingTask(_cancellationToken.Token).Forget();
		}

		public void Stop()
		{
			_cancellationToken.Cancel();
		}

		private async UniTask HandleCancellation(GetMatchmakingTicketResult ticket, PlayfabMatchmakingConfig queueConfig)
		{
			using (await _service._mutex.AcquireAsync())
			{
				FLog.Info("HandlingTicketCancellation Reason:" + ticket.CancellationReasonString + " Ticket:" + ticket.TicketId);
				if (ticket.CancellationReasonString == "Timeout")
				{
					if (queueConfig.FailsOnTimeout)
					{
						_service.CancelLocalMatchmaking(true, ScriptLocalization.UITMatchmaking.failed_to_find_players);
						return;
					}

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
		}

		private async UniTask HandleMatched(GetMatchmakingTicketResult ticket)
		{
			using (await _service._mutex.AcquireAsync())
			{
				var result = await _service.GetMatch(ticket.MatchId, ticket.QueueName);
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
					MatchIdentifier = result.MatchId,
					RoomSetup = _setup,
					PlayerProperties = new PlayerJoinRoomProperties()
					{
						// Since this game is only going to be this ticket, all the players should be in the same team
						Team = playerTeam,
						TeamColor = colorIndex
					},
				});
			}
		}

		private async UniTask PoolingTask(CancellationToken cc)
		{
			var queueConfig = _service._gameModeService.GetMatchMakingConfigFor(_setup.SimulationConfig);
			var started = Time.time;

			while (!cc.IsCancellationRequested)
			{
				try
				{
					await UniTask.Delay(PlayfabMatchmakingService.PoolingInterval, cancellationToken: cc);

					var ticket = await _service.GetTicket(Ticket, queueConfig.QueueName);
					switch (ticket.Status)
					{
						case "Matched":
							await HandleMatched(ticket);
							_cancellationToken.Cancel();
							return;
						case "Canceled":
							await HandleCancellation(ticket, queueConfig);
							return;
						default:
							FLog.Info($"Unhandled ticket status {ticket.Status}");
							break;
					}
				}
				catch (WrappedPlayFabException ex)
				{
					FLog.Warn("Failed to pool ticket", ex);
				}

				// If playfab timeout doesn't work, so the player won't get stuck in the matchmaking screen
				FLog.Info($"Already waited {Time.time - started}s for matchmaking!");
				var maxWait = queueConfig.QueueTimeoutTimeInSeconds + 15;
				if (Time.time - started >= maxWait)
				{
					FLog.Info($"Canceling ticket because it take longer then {maxWait} seconds!");
					_service.CancelLocalMatchmaking();
					_cancellationToken.Cancel();
				}
			}
		}

		// TODO - ADD PLAYFAB ERROR HANDLING IDENTIAL TO THE ONE IN GAME BACKEND NETWORK SERVICE
	}
}