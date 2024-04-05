using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Services;
using JetBrains.Annotations;
using PlayFab;
using PlayFab.MultiplayerModels;

namespace FirstLight.Game.Services.Party
{
	/// <summary>
	/// Responsible for grouping players, to later join the same match as a Team
	/// This service uses "code" to join parties, when a player <see cref="CreateParty"/> a code is generated.
	/// This code is shared manually by the players, then his friends can call <see cref="JoinParty"/> using it.
	/// After a player is inside a party, they can <see cref="LeaveParty"/> or if is the party leader can also <see cref="Kick"/>
	///
	/// External systems can observe changes with <see cref="HasParty"/> <see cref="PartyCode"/> <see cref="Members"/>  
	/// </summary>
	public interface IPartyService
	{
		/// <summary>
		/// Create a new Party generating a code when done <seealso cref="PartyCode"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like player already have party</exception>
		UniTask CreateParty();

		/// <summary>
		/// Join a party by a previous generated <paramref name="code"/> with <see cref="CreateParty"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like party not found</exception>
		UniTask JoinParty(string code);

		/// <summary>
		/// Set ready status in a party
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like party not found</exception>
		UniTask Ready(bool ready);

		/// <summary>
		/// Leave a previous joined/created party <see cref="CreateParty"/>
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like user not in a party</exception>
		UniTask LeaveParty();

		/// <summary>
		/// Kick a player from the party <see cref="CreateParty"/> using their <paramref name="playfabID"/>
		/// Must be the Party Leader to use this method
		/// </summary>
		/// <exception cref="PartyException">throws with exceptional cases like player not in party</exception>
		UniTask Kick(string playfabID);

		/// <summary>
		/// Forces a refresh of the party state / list.
		/// </summary>
		void ForceRefresh();

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
		/// PartyCode observable, it changes when the local player create/join/leave a party
		/// It is used by <see cref="JoinParty"/>
		/// </summary>
		IObservableFieldReader<string> PartyCode { get; }


		/// <summary>
		/// Playfab lobby generated id, unique string used to identify a party
		/// Use this instead of PartyCode when you need uniqueness
		/// </summary>
		IObservableFieldReader<string> PartyID { get; }

		/// <summary>
		/// Operation in Progress Observable, it automatically changes based on calls to the service
		/// </summary>
		IObservableFieldReader<bool> OperationInProgress { get; }

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
		/// Set a lobby property
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		UniTask SetLobbyProperty(string key, string value);

		/// <summary>
		/// Set a property in the local member
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		UniTask SetMemberProperty(string key, string value);

		/// <summary>
		/// Delete a lobby property
		/// </summary>
		UniTask DeleteLobbyProperty(string key);

		/// <summary>
		/// Get local player object as <see cref="PartyMember"/> from lobby <see cref="Members"/> list
		/// </summary>
		PartyMember GetLocalMember();

		/// <summary>
		/// Return the current group size, if the player is not in a party returns 1
		/// </summary>
		int GetCurrentGroupSize();

		public delegate void OnLocalPlayerKickedHandler();

		public delegate void OnLobbyPropertiesCreatedHandler(Dictionary<string, string> searchData, Dictionary<string, string> data);

		/// <summary>
		/// Event handler that notifies when the local player got kicked
		/// </summary>
		public event OnLocalPlayerKickedHandler OnLocalPlayerKicked;
		
		public event OnLobbyPropertiesCreatedHandler OnLobbyPropertiesCreated;
	}


	/// <inheritdoc/>
	public partial class PartyService : IPartyService
	{
		/// <inheritdoc/>
		IObservableListReader<PartyMember> IPartyService.Members => Members;

		/// <inheritdoc/>
		IObservableFieldReader<string> IPartyService.PartyCode => PartyCode;

		/// <inheritdoc/>
		IObservableFieldReader<bool> IPartyService.HasParty => HasParty;

		/// <inheritdoc/>
		IObservableFieldReader<bool> IPartyService.PartyReady => PartyReady;

		/// <inheritdoc/>
		IObservableDictionaryReader<string, string> IPartyService.LobbyProperties => LobbyProperties;

		/// <inheritdoc/>
		IObservableFieldReader<string> IPartyService.PartyID => PartyID;

		/// <inheritdoc/>
		IObservableFieldReader<bool> IPartyService.OperationInProgress => OperationInProgress;

		// Services
		private IPlayfabPubSubService _pubsub;
		private IPlayerDataProvider _playerDataProvider;
		private IAppDataProvider _appDataProvider;
		private IGameBackendService _backendService;
		private IGenericDialogService _genericDialogService;
		private LocalPrefsService _localPrefsService;

		// State
		private string _lobbyId;
		private PlayFabAuthenticationContext usedPlayfabContext;
		SemaphoreSlim _accessSemaphore = new (1, 1);

		private IObservableField<bool> HasParty { get; }
		private IObservableField<bool> PartyReady { get; }
		private IObservableField<bool> OperationInProgress { get; }
		private IObservableField<string> PartyCode { get; }
		private IObservableList<PartyMember> Members { get; }

		private IObservableDictionary<string, string> LobbyProperties { get; }

		private IObservableField<string> PartyID { get; }


		public PartyService(IPlayfabPubSubService pubsub,
							IPlayerDataProvider playerDataProvider,
							IAppDataProvider appDataProvider,
							IGameBackendService backendService,
							IGenericDialogService genericDialogService,
							IMessageBrokerService msgBroker,
							LocalPrefsService localPrefsService)
		{
			_playerDataProvider = playerDataProvider;
			_appDataProvider = appDataProvider;
			_pubsub = pubsub;
			_backendService = backendService;
			_genericDialogService = genericDialogService;
			_localPrefsService = localPrefsService;
			Members = new ObservableList<PartyMember>(new ());
			HasParty = new ObservableField<bool>(false);
			PartyReady = new ObservableField<bool>(false);
			OperationInProgress = new ObservableField<bool>(false);
			PartyCode = new ObservableField<string>(null);
			PartyID = new ObservableField<string>(null);
			LobbyProperties = new ObservableDictionary<string, string>(new Dictionary<string, string>());
			usedPlayfabContext = new PlayFabAuthenticationContext();
			msgBroker.Subscribe<SuccessAuthentication>(OnSuccessAuthentication);
			_pubsub.OnReconnected += () =>
			{
				OnReconnectPubSub().Forget();
			};
			msgBroker.Subscribe<ChangedServerRegionMessage>(OnChangedPhotonServer);
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
			if (HasParty.Value)
			{
				FLog.Warn("Should leave party");
				LeaveParty().Forget();
			}
		}

		/// <inheritdoc/>
		public async UniTask CreateParty()
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				OperationInProgress.Value = true;
				if (HasParty.Value)
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
				var data = new Dictionary<string, string>();
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
				usedPlayfabContext.CopyFrom(PlayFabSettings.staticPlayer);
				await FetchPartyAndUpdateState();
				// Somehow the user is not a member anymore
				if (_lobbyId == null)
				{
					return;
				}
				ConnectToPubSub(_lobbyId).Forget();
				PartyCode.Value = code;
				PartyID.Value = _lobbyId;
				HasParty.Value = true;
				PartyReady.Value = true;
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				OperationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("Create");
		}

		/// <inheritdoc/>
		public async UniTask JoinParty(string code)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				OperationInProgress.Value = true;
				if (HasParty.Value)
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

				if (FeatureFlags.COMMIT_VERSION_LOCK && lobby.SearchData.TryGetValue(LobbyCommitProperty, out var lobbyCommit))
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
					usedPlayfabContext.CopyFrom(PlayFabSettings.staticPlayer);
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
				HasParty.Value = true;
				PartyCode.Value = normalizedCode;
				PartyID.Value = _lobbyId;
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				OperationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("Join");
		}

		public async UniTask SetLobbyProperty(string key, string value)
		{
			if (!HasParty.Value)
			{
				throw new PartyException(PartyErrors.NoParty);
			}

			if (!LocalPartyMember().Leader)
			{
				throw new PartyException(PartyErrors.NoPermission);
			}

			FLog.Verbose($"setting property {key} to " + _lobbyId);
			await AsyncPlayfabAPI.UpdateLobby(new UpdateLobbyRequest()
			{
				LobbyId = _lobbyId,
				LobbyData = new Dictionary<string, string>()
				{
					{key, value}
				}
			});
		}

		public async UniTask DeleteLobbyProperty(string key)
		{
			if (!HasParty.Value)
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
		public async UniTask Ready(bool ready)
		{
			await SetMemberProperty(ReadyMemberProperty, ready.ToString());
			SendAnalyticsAction($"Ready {ready}");
		}

		/// <inheritdoc/>
		public async UniTask SetMemberProperty(string key, string value)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				OperationInProgress.Value = true;
				if (!HasParty.Value)
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
					Members.InvokeUpdate(Members.IndexOf(localPartyMember));
				}
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
			finally
			{
				OperationInProgress.Value = false;
				_accessSemaphore.Release();
			}
		}

		public UniTask Kick(string playfabID)
		{
			return Kick(playfabID, true);
		}

		private async UniTask Kick(string playfabID, bool useSemaphore, bool preventRejoin = true)
		{
			try
			{
				if (useSemaphore) await _accessSemaphore.WaitAsync();
				OperationInProgress.Value = true;
				if (!HasParty.Value)
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
				OperationInProgress.Value = false;
				if (useSemaphore) _accessSemaphore.Release();
			}

			SendAnalyticsAction("Kick");
		}

		/// <inheritdoc/>
		public async UniTask LeaveParty()
		{
			var lobbyId = _lobbyId;
			var members = MembersAsString();
			try
			{
				await _accessSemaphore.WaitAsync();
				OperationInProgress.Value = true;
				if (!HasParty.Value)
				{
					throw new PartyException(PartyErrors.NoParty);
				}


				var req = new LeaveLobbyRequest()
				{
					LobbyId = _lobbyId,
					MemberEntity = new EntityKey() {Id = usedPlayfabContext.EntityId, Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE},
					AuthenticationContext = usedPlayfabContext
				};
				// If the member counts is 0 the lobby will be disbanded and we will not need to unsubscribe
				if (Members.Count > 1)
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
				OperationInProgress.Value = false;
				_accessSemaphore.Release();
			}

			SendAnalyticsAction("Leave", lobbyId, members);
		}

		public async void ForceRefresh()
		{
			if (HasParty.Value)
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
			return HasParty.Value ? Members.Count : 1;
		}

		public event IPartyService.OnLocalPlayerKickedHandler OnLocalPlayerKicked;
		public event IPartyService.OnLobbyPropertiesCreatedHandler OnLobbyPropertiesCreated;

		private void CheckPartyReadyStatus()
		{
			PartyReady.Value = Members.Count == 1 || Members.Where(m => !m.Leader).ToList().TrueForAll(m => m.Ready);
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
				if (err == PartyErrors.UserIsNotMember)
				{
					// This means that the player got kicked before getting the connection handler of the lobby
					Members.Remove(LocalPartyMember());
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
			foreach (var (key, value) in new Dictionary<string, string>(LobbyProperties))
			{
				if (lobby?.LobbyData != null && lobby.LobbyData.TryGetValue(key, out var newValue))
				{
					if (newValue != value)
					{
						LobbyProperties[key] = newValue;
					}
				}
				else
				{
					LobbyProperties.Remove(key);
				}
			}

			// Insert
			if (lobby?.LobbyData == null) return;

			foreach (var (key, value) in lobby.LobbyData)
			{
				if (!LobbyProperties.ContainsKey(key))
				{
					LobbyProperties.Add(key, value);
				}
			}
		}

		private void UpdateMembers(Lobby lobby)
		{
			// Remove members from the list
			foreach (var partyMember in Members.ToList())
			{
				bool exists = lobby.Members.Exists(m => m.MemberEntity.Id == partyMember.PlayfabID);
				if (!exists)
				{
					Members.Remove(partyMember);
					if (partyMember.Local)
					{
						// We don't care about this result
						UnsubscribeToLobbyUpdates().Forget();
						LocalPlayerKicked();
						return;
					}
				}
			}

			// Update or Add members
			foreach (var lobbyMember in lobby.Members)
			{
				var generatedMember = ToPartyMember(lobby, lobbyMember);
				var mappedMember = Members.Where(m => m.PlayfabID == generatedMember.PlayfabID).ToArray();
				// Member already in party
				if (mappedMember.Any())
				{
					var alreadyCreatedMember = mappedMember.First();
					if (!alreadyCreatedMember.Equals(generatedMember))
					{
						generatedMember.CopyPropertiesShallowTo(alreadyCreatedMember);
						Members.InvokeUpdate(Members.IndexOf(alreadyCreatedMember));
					}
				}
				else
				{
					// ADD IT
					Members.Add(generatedMember);
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
			usedPlayfabContext = new PlayFabAuthenticationContext();
			_lobbyTopic = null;
			_lobbyChangeNumber = 0;
			HasParty.Value = false;
			PartyReady.Value = false;
			PartyCode.Value = null;
			PartyID.Value = null;
			if (_lobbyTopic != null)
			{
				_pubsub.ClearListeners(_lobbyTopic);
			}

			_pubSubState = PartySubscriptionState.NotConnected;
			Members.Clear();
			foreach (var key in new List<string>(LobbyProperties.ReadOnlyDictionary.Keys))
			{
				LobbyProperties.Remove(key);
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
			else if (Members is {Count: > 0})
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