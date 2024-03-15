using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using I2.Loc;
using JetBrains.Annotations;
using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Services.Party
{
	public partial class PartyService
	{
		private PartySubscriptionState _pubSubState = PartySubscriptionState.NotConnected;
		private string _lobbyTopic;
		private string _subscribedLobbyId;
		private uint _lobbyChangeNumber = 0;
		private SemaphoreSlim _pubSubSemaphore = new (1, 1);
		private int listenForLobbyUpdateFails = 0;


		private async UniTaskVoid OnReconnectPubSub()
		{
			if (!HasParty.Value) return;
			try
			{
				await FetchPartyAndUpdateState();
			}
			catch (PartyException ex)
			{
				FLog.Warn("party", ex);
			}

			// play lost party while was disconnected
			if (!HasParty.Value)
			{
				// TODO: Translation
				_genericDialogService.OpenButtonDialog("Squad", "You left your squad due to timeout", true, new GenericDialogButton());
			}
			else
			{
				// User still on the lobby so lets reconnect
				await ConnectToPubSub(_lobbyId);
			}
		}

		private async UniTask ConnectToPubSub(string lobbyId)
		{
			try
			{
				if (_pubSubState != PartySubscriptionState.NotConnected)
				{
					// If somehow the old one still connected let disconnect it
					await UnsubscribeToLobbyUpdates();
				}

				await _pubSubSemaphore.WaitAsync();
				_pubSubState = PartySubscriptionState.FetchConnectionURL;
				var connectionHandle = await _pubsub.GetConnectionHandle(true);

				if (lobbyId != _lobbyId)
				{
					// If the player left the party before the get connection handle function returned just ignore it
					return;
				}

				_pubSubState = PartySubscriptionState.Connecting;
				var subscribeReq = new SubscribeToLobbyResourceRequest()
				{
					Type = SubscriptionType.LobbyChange,
					EntityKey = new EntityKey() {Type = PlayFabSettings.staticPlayer.EntityType, Id = PlayFabSettings.staticPlayer.EntityId},
					PubSubConnectionHandle = connectionHandle,
					SubscriptionVersion = 1,
					ResourceId = _lobbyId
				};

				try
				{
					var result = await AsyncPlayfabAPI.SubscribeToLobbyResource(subscribeReq);
					// This whole process of creating an websocket subscribing to resource can take a a considerable time(+- 5s)
					// So we may have missed some messages and this results in a broke state
					if (lobbyId != _lobbyId)
					{
						// If the player left the party before the get connection handle function returned just ignore it
						return;
					}

					await FetchPartyAndUpdateState();
					_lobbyTopic = result.Topic;
					_subscribedLobbyId = lobbyId;
					_pubsub.ListenTopic<LobbyPayloadMessage>(_lobbyTopic, LobbyMessageHandler);
					_pubsub.ListenSubscriptionStatus(_lobbyTopic, SubscriptionChangeHandler);
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

				_pubSubState = PartySubscriptionState.Connected;
				// We may lost some messages while the connection is being established, so lets update the lobby manually again just to be safe
				await FetchPartyAndUpdateState();
			}
			catch (Exception ex)
			{
				// THIS EXCEPTION IS VERY IMPORTANT! IF THIS FAILS SQUADS NOT WORK AT ALL!
				_pubSubState = PartySubscriptionState.NotConnected;
				listenForLobbyUpdateFails++;
				if (listenForLobbyUpdateFails > 2)
				{
					_backendService.HandleUnrecoverableException(ex, AnalyticsCallsErrors.ErrorType.Squads);
					return;
				}

				_genericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, PartyErrors.Unknown.GetTranslation(), true,
					new GenericDialogButton());
				FLog.Error($"failed subscribing to lobby notifications: {ex.Message}", ex);
				// Lets leave the party so the player can try again
				try
				{
					await LeaveParty();
				}
				catch (Exception)
				{
					// ignored, because because it is just an attempt to make the state valid
				}
			}
			finally
			{
				_pubSubSemaphore.Release();
			}
		}

		private void ResetPubSubState()
		{
			_pubSubState = PartySubscriptionState.NotConnected;
			_subscribedLobbyId = null;
		}


		private void SubscriptionChangeHandler(IPlayfabPubSubService.SubscriptionChangeMessage obj)
		{
			if (obj.Status == "unsubscribeSuccess" && _memberRemovedReasons.Contains(obj.UnsubscribeReason))
			{
				Members.Remove(LocalPartyMember());
				LocalPlayerKicked();
				ResetPubSubState();
			}
		}


		/// <summary>
		/// Handles when there is any change in the party
		/// </summary>
		/// <param name="obj"></param>
		private void LobbyMessageHandler(LobbyPayloadMessage obj)
		{
			// Let executes async so we can wait for fetch party and use semaphore properly
			UpdateLobbyState(obj).Forget();
		}


		private async UniTask UpdateLobbyState(LobbyPayloadMessage obj)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				if (_lobbyId == null) return;
				foreach (var change in obj.lobbyChanges)
				{
					if (_lobbyChangeNumber >= change.changeNumber) continue;
					// If by any means we lost a message
					if (change.changeNumber - _lobbyChangeNumber >= 2)
					{
						// Fetch from HTTP the latest state
						FLog.Warn("Fetching state from HTTP because desync!");
						await FetchPartyAndUpdateState();
						return;
					}

					ApplyLobbyChangeIntoMembers(change);
					ApplyLobbyChangeIntoProperties(change);
					CheckPartyReadyStatus();
					_lobbyChangeNumber = change.changeNumber;
				}
			}
			finally
			{
				_accessSemaphore.Release();
			}
		}

		private void ApplyLobbyChangeIntoProperties(LobbyChange change)
		{
			if (change.lobbyDataToDelete != null)
			{
				foreach (var s in change.lobbyDataToDelete)
				{
					if (LobbyProperties.ReadOnlyDictionary.ContainsKey(s))
					{
						LobbyProperties.Remove(s);
					}
				}
			}

			if (change?.lobbyData == null || change.lobbyData.Count == 0) return;

			foreach (var (key, value) in change.lobbyData)
			{
				if (LobbyProperties.ReadOnlyDictionary.ContainsKey(key))
				{
					LobbyProperties[key] = value;
				}
				else
				{
					LobbyProperties.Add(key, value);
				}
			}
		}

		private void ApplyLobbyChangeIntoMembers(LobbyChange change)
		{
			HashSet<string> invokeUpdates = new ();
			if (change.memberToDelete != null)
			{
				var member = Members.FirstOrDefault(m => m.PlayfabID == change.memberToDelete.memberEntity.Id);
				if (member != null)
				{
					Members.Remove(member);
					if (member.Local)
					{
						UnsubscribeToLobbyUpdates().Forget();
						LocalPlayerKicked();
						return;
					}
				}
			}


			// Let's check to add players to the party
			if (change.memberToMerge != null)
			{
				var currentOwner = Members.FirstOrDefault(m => m.Leader)?.PlayfabID;
				var owner = change.owner != null ? change.owner.Id : (currentOwner ?? "");
				var localMember = Members.ReadOnlyList.FirstOrDefault(m => m.PlayfabID == change.memberToMerge.memberEntity.Id);

				if (localMember == null)
				{
					var newMember = ToPartyMember(change.memberToMerge, owner == change.memberToMerge.memberEntity.Id);
					Members.Add(newMember);
				}
				else
				{
					// MERGE NOT COPY
					if (change.memberToMerge.memberData?.Count != 0)
					{
						if (MergeData(localMember, change.memberToMerge.memberData))
						{
							invokeUpdates.Add(localMember.PlayfabID);
						}
					}
				}
			}

			// Lets check if the owner changed
			if (change.owner != null)
			{
				// Search of old owner
				var currentOwner = Members.ReadOnlyList.FirstOrDefault(m => m.Leader);
				if (currentOwner != null && currentOwner.PlayfabID != change.owner.Id)
				{
					currentOwner.Leader = false;
					invokeUpdates.Add(currentOwner.PlayfabID);
				}

				// Lets find the new owner
				var newOwner = Members.ReadOnlyList.FirstOrDefault(m => m.PlayfabID == change.owner.Id);
				if (newOwner is {Leader: false})
				{
					newOwner.Ready = false;
					newOwner.Leader = true;
					invokeUpdates.Add(newOwner.PlayfabID);
				}
			}

			// Player lost connection 
			if (change.memberToMerge is {noPubSubConnectionHandle: true})
			{
				var disconnectedMember = Members.FirstOrDefault(m => m.PlayfabID == change.memberToMerge.memberEntity.Id);
				if (disconnectedMember != null)
				{
					// Let the leader kick it
					if (LocalPartyMember() is {Leader: true})
					{
						KickDisconnectedPlayer(disconnectedMember.PlayfabID).Forget();
					}
				}
			}

			foreach (var invokeUpdate in invokeUpdates)
			{
				var member = Members.FirstOrDefault(m => m.PlayfabID == invokeUpdate);
				if (member == null)
				{
					continue;
				}

				Members.InvokeUpdate(Members.IndexOf(member));
			}
		}

		private async UniTaskVoid KickDisconnectedPlayer(string playfabId)
		{
			try
			{
				await Kick(playfabId, false, false);
			}
			catch (PartyException ex)
			{
				// Ignore it because it is not an action that the player manually did
				FLog.Warn("failed to kick disconnected player", ex);
			}
		}

		private async UniTask UnsubscribeToLobbyUpdates()
		{
			if (_pubSubState != PartySubscriptionState.Connected || _subscribedLobbyId == null)
			{
				return;
			}

			try
			{
				await _pubSubSemaphore.WaitAsync();

				var connStr = await _pubsub.GetConnectionHandle();
				var req = new UnsubscribeFromLobbyResourceRequest()
				{
					Type = SubscriptionType.LobbyChange,
					EntityKey = LocalEntityKey(),
					ResourceId = _subscribedLobbyId,
					SubscriptionVersion = 1,
					PubSubConnectionHandle = connStr
				};

				await AsyncPlayfabAPI.UnsubscribeFromLobbyResource(req);
			}
			catch (Exception ex)
			{
				// Ignore error because if the player is the last one in the lobby it will disband it and automatically unsubscribe from updates
				FLog.Info("Ignoring exception: " + ex);
			}
			finally
			{
				_pubSubSemaphore.Release();
				ResetPubSubState();
			}
		}
	}
}