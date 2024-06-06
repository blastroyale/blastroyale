using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Services;
using JetBrains.Annotations;
using NUnit.Framework;
using PlayFab;
using PlayFab.MultiplayerModels;
using Unity.Services.Authentication;

namespace FirstLight.Game.Services.Party
{
	public interface IPartyService
	{
		/// <summary>
		/// HasParty observable, it changes when the local player join/leave a party
		/// If this value change to false, it means the player left/got kicked from the party.
		/// To distinct the two you should look at <see cref="Members"/>
		/// </summary>
		IObservableFieldReader<bool> HasParty { get; }

		/// <summary>
		/// Party ready status observable, it changes when the all party Members has it property Ready equals true
		/// </summary>
		IObservableFieldReader<bool> PartyReady { get; }

		/// <summary>
		/// Operation in Progress Observable, it automatically changes based on calls to the service
		/// </summary>
		IObservableFieldReader<bool> OperationInProgress { get; }

		/// <summary>
		/// PartyCode observable, it changes when the local player create/join/leave a party
		/// It is used by <see cref="JoinParty"/>
		/// </summary>
		IObservableFieldReader<string> PartyCode { get; }

		/// <summary>
		/// The members of the local player party, it changes when any player join/leave the party
		/// If the local player removed from this list and <see cref="HasParty"/> is true, means the player was kicked.
		/// </summary>
		IObservableListReader<PartyMember> Members { get; }

		/// <summary>
		/// Private properties of a lobby, this is only settable by the leader
		/// </summary>
		IObservableDictionaryReader<string, string> LobbyProperties { get; }

		/// <summary>
		/// Ready status of the local player, it has a buffer so we don't flood the network
		/// </summary>
		IObservableFieldReader<bool> LocalReadyStatus { get; }

		/// <summary>
		/// Create a new Party generating a code when done <seealso cref="PartyService.PartyCode"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like player already have party</exception>
		UniTask CreateParty();

		/// <summary>
		/// Join a party by a previous generated <paramref name="code"/> with <see cref="PartyService.CreateParty"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like party not found</exception>
		UniTask JoinParty(string code);

		/// <summary>
		/// Set a property of the lobby, you must be the leader
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="bumpReadyVersion"></param>
		/// <returns></returns>
		UniTask SetLobbyProperty(string key, string value, bool bumpReadyVersion);

		/// <summary>
		/// Delete a lobby property, must be the leader
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		UniTask DeleteLobbyProperty(string key);

		/// <summary>
		/// Set a property for the local player
		/// </summary>
		UniTask SetMemberProperty(string key, string value);

		/// <summary>
		/// Kick a player from the party <see cref="PartyService.CreateParty"/> using their <paramref name="playfabID"/>
		/// Must be the Party Leader to use this method
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like player not in party</exception>
		UniTask Kick(string playfabID);

		/// <summary>
		/// Promote a player to leader of the party, local player must be the current leader
		/// </summary>
		/// <param name="playfabID"></param>
		UniTask Promote(string playfabID);

		/// <summary>
		/// Leave a previous joined/created party <see cref="PartyService.CreateParty"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like user not in a party</exception>
		UniTask LeaveParty();

		/// <summary>
		/// Forces a refresh of the party state / list.
		/// </summary>
		void ForceRefresh();

		/// <summary>
		/// Get the local player PartyMemberObject
		/// </summary>
		/// <returns></returns>
		PartyMember GetLocalMember();

		/// <summary>
		/// Get the current size of the player party, if the player is not in a party the value is one
		/// </summary>
		/// <returns></returns>
		int GetCurrentGroupSize();

		/// <summary>
		/// Trigerred when the local player gets kicked of the party
		/// </summary>
		event Action OnLocalPlayerKicked;

		/// <summary>
		/// Event that allows to overwrite lobby properties on creation, so other services can inject properties
		/// </summary>
		event PartyService.OnLobbyPropertiesCreatedHandler OnLobbyPropertiesCreated;

		/// <summary>
		/// Check if a member of the lobby is ready
		/// </summary>
		/// <param name="member"></param>
		/// <param name="localBuffered"></param>
		/// <returns></returns>
		bool IsReady(PartyMember member, bool localBuffered = true);

		/// <summary>
		/// Set ready status in a party
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like party not found</exception>
		UniTask Ready(bool ready);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ready"></param>
		/// <returns></returns>
		UniTask BufferedReady(bool ready);
	}

	/// <summary>
	/// Responsible for grouping players, to later join the same match as a Team
	/// This service uses "code" to join parties, when a player <see cref="CreateParty"/> a code is generated.
	/// This code is shared manually by the players, then his friends can call <see cref="JoinParty"/> using it.
	/// After a player is inside a party, they can <see cref="LeaveParty"/> or if is the party leader can also <see cref="Kick"/>
	///
	/// External systems can observe changes with <see cref="_hasParty"/> <see cref="_partyCode"/> <see cref="_members"/>  
	/// </summary>
	public partial class PartyService : IPartyService
	{
		// Services
		private IPlayfabPubSubService _pubsub;
		private IAppDataProvider _appDataProvider;
		private IGameBackendService _backendService;
		private IGenericDialogService _genericDialogService;
		private LocalPrefsService _localPrefsService;

		// State
		private string _lobbyId;
		private PlayFabAuthenticationContext _usedPlayfabContext;
		SemaphoreSlim _accessSemaphore = new (1, 1);

		private readonly IObservableField<bool> _hasParty;
		public IObservableFieldReader<bool> HasParty => _hasParty;

		private readonly IObservableField<bool> _partyReady;
		public IObservableFieldReader<bool> PartyReady => _partyReady;

		private readonly IObservableField<bool> _operationInProgress;
		public IObservableFieldReader<bool> OperationInProgress => _operationInProgress;

		private readonly IObservableField<string> _partyCode;
		public IObservableFieldReader<string> PartyCode => _partyCode;

		private readonly IObservableList<PartyMember> _members;
		public IObservableListReader<PartyMember> Members => _members;

		private readonly IObservableDictionary<string, string> _lobbyProperties;
		public IObservableDictionaryReader<string, string> LobbyProperties => _lobbyProperties;

		private readonly IObservableField<bool> _localReadyStatus;
		IObservableFieldReader<bool> IPartyService.LocalReadyStatus => _localReadyStatus;

		public PartyService(IPlayfabPubSubService pubsub,
							IAppDataProvider appDataProvider,
							IGameBackendService backendService,
							IGenericDialogService genericDialogService,
							IMessageBrokerService msgBroker,
							LocalPrefsService localPrefsService)
		{
			_appDataProvider = appDataProvider;
			_pubsub = pubsub;
			_backendService = backendService;
			_genericDialogService = genericDialogService;
			_localPrefsService = localPrefsService;
			_members = new ObservableList<PartyMember>(new ());
			_hasParty = new ObservableField<bool>(false);
			_localReadyStatus = new ObservableField<bool>(false);
			_partyReady = new ObservableField<bool>(false);
			_operationInProgress = new ObservableField<bool>(false);
			_partyCode = new ObservableField<string>(null);
			_lobbyProperties = new ObservableDictionary<string, string>(new Dictionary<string, string>());
			_usedPlayfabContext = new PlayFabAuthenticationContext();
			msgBroker.Subscribe<SuccessAuthentication>(OnSuccessAuthentication);
			_pubsub.OnReconnected += () =>
			{
				OnReconnectPubSub().Forget();
			};
			msgBroker.Subscribe<ChangedServerRegionMessage>(OnChangedPhotonServer);
			msgBroker.Subscribe<CollectionItemEquippedMessage>(OnCharacterSkinUpdatedMessage);
			msgBroker.Subscribe<TrophiesUpdatedMessage>(OnTrophiesUpdateMessage);
			// TODO mihak: _appDataProvider.DisplayName.Observe(OnDisplayNameChanged);

			_lobbyProperties.Observe(ReadyVersion, OnReadyVersionChanged);
		}

		private void OnDisplayNameChanged(string _, string _2)
		{
			if (!_hasParty.Value)
			{
				return;
			}

			SetMemberProperty(PartyMember.DISPLAY_NAME_MEMBER_PROPERTY, AuthenticationService.Instance.PlayerName).Forget();
		}

		private void OnCharacterSkinUpdatedMessage(CollectionItemEquippedMessage obj)
		{
			if (!_hasParty.Value)
			{
				return;
			}

			if (obj.Category == CollectionCategories.PLAYER_SKINS)
			{
				SetMemberProperty(PartyMember.CHARACTER_SKIN_PROPERTY, obj.EquippedItem.Id.ToString()).Forget();
			}

			if (obj.Category == CollectionCategories.MELEE_SKINS)
			{
				SetMemberProperty(PartyMember.MELEE_SKIN_PROPERTY, obj.EquippedItem.Id.ToString()).Forget();
			}
		}

		private void OnTrophiesUpdateMessage(TrophiesUpdatedMessage obj)
		{
			if (!_hasParty.Value)
			{
				return;
			}

			SetMemberProperty(PartyMember.TROPHIES_PROPERTY, obj.NewValue.ToString()).Forget();
		}

		private void OnChangedPhotonServer(ChangedServerRegionMessage obj)
		{
			LeavePartyAndForget();
		}

		private void OnSuccessAuthentication(SuccessAuthentication obj)
		{
			LeavePartyAndForget();
		}

		private void LeavePartyAndForget()
		{
			if (_hasParty.Value)
			{
				FLog.Warn("Should leave party");
				LeaveParty().Forget();
			}
		}

		/// <summary>
		/// Create a new Party generating a code when done <seealso cref="_partyCode"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like player already have party</exception>
		public async UniTask CreateParty()
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				_operationInProgress.Value = true;
				if (_hasParty.Value)
				{
					throw new PartyException(PartyErrors.AlreadyInParty);
				}

				var code = JoinCodeUtils.GenerateCode(CodeDigits);
				// TODO Check if lobby doesn't exist with the generated code
				var server = _localPrefsService.ServerRegion;

				var searchData = new Dictionary<string, string>()
				{
					{CodeSearchProperty, code},
					{ServerProperty, server},
					{LobbyCommitProperty, VersionUtils.Commit != null ? VersionUtils.Commit : "editor"},
				};
				var data = new Dictionary<string, string>()
				{
					{ReadyVersion, "1"}
				};
				OnLobbyPropertiesCreated?.Invoke(searchData, data);
				CreateLobbyRequest req = new CreateLobbyRequest()
				{
					Owner = LocalEntityKey(),
					AccessPolicy = AccessPolicy.Public,
					MaxPlayers = MaxMembers,
					OwnerMigrationPolicy = OwnerMigrationPolicy.Automatic,
					SearchData = searchData,
					LobbyData = data,
					UseConnections = true,
					Members = new List<Member> {CreateLocalMember()}
				};
				var result = await AsyncPlayfabAPI.CreateLobby(req);
				_lobbyId = result.LobbyId;
				_usedPlayfabContext.CopyFrom(PlayFabSettings.staticPlayer);
				await FetchPartyAndUpdateState();
				// Somehow the user is not a member anymore
				if (_lobbyId == null)
				{
					return;
				}

				ConnectToPubSub(_lobbyId).Forget();
				_partyCode.Value = code;
				_hasParty.Value = true;
				_partyReady.Value = true;
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				_operationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("Create");
		}

		/// <summary>
		/// Join a party by a previous generated <paramref name="code"/> with <see cref="CreateParty"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like party not found</exception>
		public async UniTask JoinParty(string code)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				_operationInProgress.Value = true;
				if (_hasParty.Value)
				{
					throw new PartyException(PartyErrors.AlreadyInParty);
				}

				var normalizedCode = JoinCodeUtils.NormalizeCode(code);

				if (normalizedCode.Length != CodeDigits || normalizedCode.Any(c => !JoinCodeUtils.AllowedCharacters.Contains(c)))
				{
					throw new PartyException(PartyErrors.PartyNotFound);
				}

				var filter = $"{CodeSearchProperty} eq '{normalizedCode}'";
				var req = new FindLobbiesRequest()
				{
					Filter = filter
				};
				var lobbiesResult = await AsyncPlayfabAPI.FindLobbies(req);
				// TODO players can create any code they want(doing request manually curl),
				// so it is possible to have multiple lobbies with the same code 

				var lobby = lobbiesResult.Lobbies.FirstOrDefault();
				if (lobby == null)
				{
					throw new PartyException(PartyErrors.PartyNotFound);
				}

				if (lobby.SearchData.TryGetValue(ServerProperty, out var lobbyServer))
				{
					var server = _localPrefsService.ServerRegion.Value;
					if (lobbyServer != server)
					{
						throw new PartyException(PartyErrors.PartyUsingOtherServer);
					}
				}

				if (RemoteConfigs.Instance.EnableCommitVersionLock && lobby.SearchData.TryGetValue(LobbyCommitProperty, out var lobbyCommit))
				{
					if (lobbyCommit != VersionUtils.Commit)
					{
						throw new PartyException(PartyErrors.DifferentGameVersion);
					}
				}

				var localMember = CreateLocalMember();
				// Now join it
				var joinRequest = new JoinLobbyRequest()
				{
					ConnectionString = lobby.ConnectionString,
					MemberEntity = localMember.MemberEntity,
					MemberData = localMember.MemberData
				};
				try
				{
					var result = await AsyncPlayfabAPI.JoinLobby(joinRequest);
					_lobbyId = result.LobbyId;
					_usedPlayfabContext.CopyFrom(PlayFabSettings.staticPlayer);
				}
				catch (WrappedPlayFabException ex)
				{
					// If the player quit the games and try to join the same party will return this error
					var error = ex.Error.Error;
					if (error != PlayFabErrorCode.LobbyPlayerAlreadyJoined)
					{
						HandleException(ex);
					}

					_lobbyId = lobby.LobbyId;
				}

				await FetchPartyAndUpdateState();
				ConnectToPubSub(_lobbyId).Forget();
				_hasParty.Value = true;
				_partyCode.Value = normalizedCode;
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				_operationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("Join");
		}

		public async UniTask SetLobbyProperty(string key, string value, bool bumpReadyVersion)
		{
			if (!_hasParty.Value)
			{
				throw new PartyException(PartyErrors.NoParty);
			}

			if (!LocalPartyMember().Leader)
			{
				throw new PartyException(PartyErrors.NoPermission);
			}

			FLog.Verbose($"setting property {key} to " + _lobbyId);
			var lobbyData = new Dictionary<string, string>()
			{
				{key, value}
			};
			if (bumpReadyVersion)
			{
				lobbyData[ReadyVersion] = $"{int.Parse(_lobbyProperties[ReadyVersion]) + 1}";
			}

			await AsyncPlayfabAPI.UpdateLobby(new UpdateLobbyRequest()
			{
				LobbyId = _lobbyId,
				LobbyData = lobbyData
			});
		}

		public async UniTask DeleteLobbyProperty(string key)
		{
			if (!_hasParty.Value)
			{
				throw new PartyException(PartyErrors.NoParty);
			}

			if (!LocalPartyMember().Leader)
			{
				throw new PartyException(PartyErrors.NoPermission);
			}

			FLog.Verbose($"removing property {key} from " + _lobbyId);
			await AsyncPlayfabAPI.UpdateLobby(new UpdateLobbyRequest()
			{
				LobbyId = _lobbyId,
				LobbyDataToDelete = new List<string> {key}
			});
		}

		/// <inheritdoc/>
		public async UniTask SetMemberProperty(string key, string value)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				_operationInProgress.Value = true;
				if (!_hasParty.Value)
				{
					throw new PartyException(PartyErrors.NoParty);
				}

				var localPartyMember = LocalPartyMember();
				if (localPartyMember == null)
				{
					throw new PartyException(PartyErrors.NoPermission);
				}

				var data = new Dictionary<string, string>()
				{
					{key, value}
				};

				await AsyncPlayfabAPI.UpdateLobby(new UpdateLobbyRequest()
				{
					LobbyId = _lobbyId,
					MemberEntity = localPartyMember.ToEntityKey(),
					MemberData = data
				});
				// If the request was successful let's update locally so 
				if (MergeData(localPartyMember, data))
				{
					_members.InvokeUpdate(_members.IndexOf(localPartyMember));
				}
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				_operationInProgress.Value = false;
				_accessSemaphore.Release();
			}
		}

		/// <summary>
		/// Kick a player from the party <see cref="CreateParty"/> using their <paramref name="playfabID"/>
		/// Must be the Party Leader to use this method
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like player not in party</exception>
		public UniTask Kick(string playfabID)
		{
			return Kick(playfabID, true);
		}

		private async UniTask Kick(string playfabID, bool useSemaphore, bool preventRejoin = true)
		{
			try
			{
				if (useSemaphore) await _accessSemaphore.WaitAsync();
				_operationInProgress.Value = true;
				if (!_hasParty.Value)
				{
					throw new PartyException(PartyErrors.NoParty);
				}

				var localPartyMember = LocalPartyMember();
				if (localPartyMember == null || !localPartyMember.Leader)
				{
					throw new PartyException(PartyErrors.NoPermission);
				}

				var req = new RemoveMemberFromLobbyRequest()
				{
					LobbyId = _lobbyId,
					PreventRejoin = preventRejoin,
					MemberEntity = new EntityKey() {Id = playfabID, Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE}
				};
				await AsyncPlayfabAPI.RemoveMember(req);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				_operationInProgress.Value = false;
				if (useSemaphore) _accessSemaphore.Release();
			}

			SendAnalyticsAction("Kick");
		}

		public async UniTask Promote(string playfabID)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				_operationInProgress.Value = true;
				if (!_hasParty.Value)
				{
					throw new PartyException(PartyErrors.NoParty);
				}

				var localPartyMember = LocalPartyMember();
				if (localPartyMember == null || !localPartyMember.Leader)
				{
					throw new PartyException(PartyErrors.NoPermission);
				}

				var req = new UpdateLobbyRequest()
				{
					LobbyId = _lobbyId,
					Owner = new EntityKey() {Id = playfabID, Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE}
				};
				await AsyncPlayfabAPI.UpdateLobby(req);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				_operationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("PromoteMember");
		}

		/// <summary>
		/// Leave a previous joined/created party <see cref="CreateParty"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like user not in a party</exception>
		public async UniTask LeaveParty()
		{
			var lobbyId = _lobbyId;
			var members = MembersAsString();
			try
			{
				await _accessSemaphore.WaitAsync();
				_operationInProgress.Value = true;
				if (!_hasParty.Value)
				{
					throw new PartyException(PartyErrors.NoParty);
				}

				var req = new LeaveLobbyRequest()
				{
					LobbyId = _lobbyId,
					MemberEntity = new EntityKey() {Id = _usedPlayfabContext.EntityId, Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE},
					AuthenticationContext = _usedPlayfabContext
				};
				// If the member counts is 0 the lobby will be disbanded and we will not need to unsubscribe
				if (_members.Count > 1)
				{
					await UnsubscribeToLobbyUpdates();
				}

				if (_lobbyTopic != null)
				{
					_pubsub.ClearListeners(_lobbyTopic);
				}

				try
				{
					await AsyncPlayfabAPI.LeaveLobby(req);
				}
				catch (WrappedPlayFabException ex)
				{
					var errors = ConvertErrors(ex);

					// If player try to leaves the lobby but he is not on the lobby or the lobby doesn't exists ignore the error and reset state
					if (errors is PartyErrors.UserIsNotMember or PartyErrors.TryingToGetDetailsOfNonMemberParty or PartyErrors.PartyNotFound or PartyErrors.MemberNotFound)
					{
						ResetState();
						return;
					}

					throw ex;
				}

				ResetState();
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				_operationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("Leave", lobbyId, members);
		}

		/// <summary>
		/// Forces a refresh of the party state / list.
		/// </summary>
		public async void ForceRefresh()
		{
			if (_hasParty.Value)
			{
				await FetchPartyAndUpdateState();
			}
		}

		[CanBeNull]
		public PartyMember GetLocalMember()
		{
			return LocalPartyMember();
		}

		public int GetCurrentGroupSize()
		{
			return _hasParty.Value ? _members.Count : 1;
		}

		public event Action OnLocalPlayerKicked;

		public delegate void OnLobbyPropertiesCreatedHandler(Dictionary<string, string> searchData, Dictionary<string, string> data);

		public event OnLobbyPropertiesCreatedHandler OnLobbyPropertiesCreated;

		private void CheckPartyReadyStatus()
		{
			_partyReady.Value = _members.Count == 1 || _members.Where(m => !m.Leader).ToList().TrueForAll(m => IsReady(m, false));
		}

		public bool IsReady(PartyMember member, bool localBuffered = true)
		{
			if (!_hasParty.Value) return false;
			if (member.Local && localBuffered) return _localReadyStatus.Value;
			return member.ReadyVersion == _members.FirstOrDefault(m => m.Leader)?.PlayfabID + "=" + _lobbyProperties[ReadyVersion];
		}

		private string ReadyKey()
		{
			if (!_lobbyProperties.TryGetValue(ReadyVersion, out var readyVersion))
			{
				readyVersion = "0";
			}

			return $"{_members.FirstOrDefault(m => m.Leader)?.PlayfabID}={readyVersion}";
		}

		/// <summary>
		/// Set ready status in a party
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like party not found</exception>
		public async UniTask Ready(bool ready)
		{
			await SetMemberProperty(PartyMember.READY_MEMBER_PROPERTY, ready ? ReadyKey() : "nope");
			SendAnalyticsAction($"Ready {ready}");
		}

		private CancellationTokenSource _readyBuffercancel = null;

		public async UniTask BufferedReady(bool ready)
		{
			_readyBuffercancel?.Cancel();
			_localReadyStatus.Value = ready;

			// Marking unready is instantaneously 
			if (!ready)
			{
				// If the player is already set to not ready nothing we need to do
				if (IsReady(GetLocalMember(), false))
				{
					await Ready(false);
				}

				return;
			}

			var cts = _readyBuffercancel = new CancellationTokenSource();
			await UniTask.Delay(2000, cancellationToken: cts.Token);
			if (cts.IsCancellationRequested) return;
			await Ready(true);
		}

		private void OnReadyVersionChanged(string arg1, string arg2, string arg3, ObservableUpdateType arg4)
		{
			if (!_hasParty.Value) return;
			_readyBuffercancel?.Cancel();
			_localReadyStatus.Value = GetLocalMember()?.ReadyVersion == ReadyKey();
		}

		private async UniTask FetchPartyAndUpdateState()
		{
			try
			{
				var req = new GetLobbyRequest()
				{
					LobbyId = _lobbyId,
				};
				var result = await AsyncPlayfabAPI.GetLobby(req);
				_lobbyId = result.Lobby.LobbyId;
				UpdateMembers(result.Lobby);
				UpdateProperties(result.Lobby);
				_lobbyChangeNumber = result.Lobby.ChangeNumber;
			}
			catch (WrappedPlayFabException ex)
			{
				var err = ConvertErrors(ex);
				if (err == PartyErrors.UserIsNotMember || err == PartyErrors.PartyNotFound)
				{
					ResetPubSubState();
					LocalPlayerKicked();
					return;
				}

				throw;
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		private void UpdateProperties(Lobby lobby)
		{
			// Remove/update
			foreach (var (key, value) in new Dictionary<string, string>(_lobbyProperties))
			{
				if (lobby?.LobbyData != null && lobby.LobbyData.TryGetValue(key, out var newValue))
				{
					if (newValue != value)
					{
						_lobbyProperties[key] = newValue;
					}
				}
				else
				{
					_lobbyProperties.Remove(key);
				}
			}

			// Insert
			if (lobby?.LobbyData == null) return;

			foreach (var (key, value) in lobby.LobbyData)
			{
				if (!_lobbyProperties.ContainsKey(key))
				{
					_lobbyProperties.Add(key, value);
				}
			}
		}

		private void UpdateMembers(Lobby lobby)
		{
			// Remove members from the list
			foreach (var partyMember in _members.ToList())
			{
				bool exists = lobby.Members.Exists(m => m.MemberEntity.Id == partyMember.PlayfabID);
				if (!exists)
				{
					if (partyMember.Local)
					{
						// We don't care about this result
						UnsubscribeToLobbyUpdates().Forget();
						LocalPlayerKicked();
						return;
					}

					_members.Remove(partyMember);
				}
			}

			// Update or Add members
			foreach (var lobbyMember in lobby.Members)
			{
				var generatedMember = ToPartyMember(lobby, lobbyMember);
				var mappedMember = _members.Where(m => m.PlayfabID == generatedMember.PlayfabID).ToArray();
				// Member already in party
				if (mappedMember.Any())
				{
					var alreadyCreatedMember = mappedMember.First();
					if (!alreadyCreatedMember.Equals(generatedMember))
					{
						generatedMember.CopyPropertiesShallowTo(alreadyCreatedMember);
						_members.InvokeUpdate(_members.IndexOf(alreadyCreatedMember));
					}
				}
				else
				{
					// ADD IT
					_members.Add(generatedMember);
				}
			}

			CheckPartyReadyStatus();
		}

		private void LocalPlayerKicked()
		{
			SendAnalyticsAction("Kicked");
			ResetState();
			OnLocalPlayerKicked?.Invoke();
		}

		private void ResetState()
		{
			_lobbyId = null;
			_usedPlayfabContext = new PlayFabAuthenticationContext();
			_lobbyTopic = null;
			_lobbyChangeNumber = 0;
			_hasParty.Value = false;
			_partyReady.Value = false;
			_partyCode.Value = null;
			_localReadyStatus.Value = false;
			if (_lobbyTopic != null)
			{
				_pubsub.ClearListeners(_lobbyTopic);
			}

			_pubSubState = PartySubscriptionState.NotConnected;
			_members.Clear();
			foreach (var key in new List<string>(_lobbyProperties.ReadOnlyDictionary.Keys))
			{
				_lobbyProperties.Remove(key);
			}
		}

		private void SendAnalyticsAction(string action, string overwriteLobbyId = null, string overwriteMembersString = null)
		{
			var lobby = overwriteLobbyId ?? _lobbyId;
			var members = "";
			if (overwriteMembersString != null)
			{
				members = overwriteMembersString;
			}
			else if (_members is {Count: > 0})
			{
				members = MembersAsString();
			}

			MainInstaller.Resolve<IGameServices>().AnalyticsService.LogEvent("team_action", new AnalyticsData()
			{
				{"action", action},
				{"user_id", PlayFabSettings.staticPlayer.PlayFabId},
				{"team_id", lobby},
				{"members", members}
			});
		}
	}
}