using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		private SemaphoreSlim _pubSubSemaphore = new(1, 1);

		private async Task ListenForLobbyUpdates(string lobbyId)
		{
			try
			{
				await _pubSubSemaphore.WaitAsync();
				if (_pubSubState != PartySubscriptionState.NotConnected)
				{
					// If somehow the old one still connected let disconnect it
					await UnsubscribeToLobbyUpdates();
				}

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
					var result = await AsyncPlayfabMultiplayerAPI.SubscribeToLobbyResource(subscribeReq);
					// This whole process of creating an websocket subscribing to resource can take a a considerable time(+- 5s)
					// So we may have missed some messages and this results in a broke state
					await FetchPartyAndUpdateState();
					_lobbyTopic = result.Topic;
					_subscribedLobbyId = lobbyId;
					_pubsub.ListenTopic<LobbyPayloadMessage>(_lobbyTopic, LobbyMessageHandler);
					_pubsub.ListenSubscriptionStatus(_lobbyTopic, SubscriptionChangeHandler);
				}
				catch (WrappedPlayFabException ex)
				{
					if (ex.Error.Error == PlayFabErrorCode.LobbyBadRequest)
					{
						if (ex.Error.ErrorMessage == "User is not lobby owner or member")
						{
							// This means that the player got kicked before getting the connection handler of the lobby
							Members.Remove(LocalPartyMember());
							ResetPubSubState();
							LocalPlayerKicked();
							return;
						}
					}

					throw;
				}

				_pubSubState = PartySubscriptionState.Connected;
			}
			catch (Exception ex)
			{
				_pubSubState = PartySubscriptionState.NotConnected;
				// Since this function is never awaited lets log the exception
				// TODO Proper handling of async exceptions
				Debug.LogException(ex);
				// TODO: THIS EXCEPTION IS VERY IMPORTANT! IF THIS FAILS SQUADS NOT WORK AT ALL!
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
#pragma warning disable CS4014
			UpdateLobbyState(obj);
#pragma warning restore CS4014
		}

		private async Task UpdateLobbyState(LobbyPayloadMessage obj)
		{
			try
			{
				await _accessSemaphore.WaitAsync();
				foreach (var change in obj.lobbyChanges)
				{
					if (_lobbyChangeNumber >= change.changeNumber) continue;
					// If by any means we lost a message
					if (change.changeNumber - _lobbyChangeNumber >= 2)
					{
						// Fetch from HTTP the latest state
						Debug.LogWarning("Fetching state from HTTP because desync!");
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
			HashSet<string> invokeUpdates = new();
			if (change.memberToDelete != null)
			{
				var member = Members.FirstOrDefault(m => m.PlayfabID == change.memberToDelete.memberEntity.Id);
				if (member != null)
				{
					Members.Remove(member);
					if (member.Local)
					{
#pragma warning disable CS4014
						UnsubscribeToLobbyUpdates();
#pragma warning restore CS4014
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

		private async Task UnsubscribeToLobbyUpdates()
		{
			try
			{
				await _pubSubSemaphore.WaitAsync();

				if (_pubSubState != PartySubscriptionState.Connected || _subscribedLobbyId == null)
				{
					return;
				}


				var connStr = await _pubsub.GetConnectionHandle();
				var req = new UnsubscribeFromLobbyResourceRequest()
				{
					Type = SubscriptionType.LobbyChange,
					EntityKey = LocalEntityKey(),
					ResourceId = _subscribedLobbyId,
					SubscriptionVersion = 1,
					PubSubConnectionHandle = connStr
				};

				await AsyncPlayfabMultiplayerAPI.UnsubscribeFromLobbyResource(req);
			}
			catch (Exception ex)
			{
				// Ignore error because if the player is the last one in the lobby it will disband it and automatically unsubscribe from updates
				Debug.LogException(ex);
			}
			finally
			{
				_pubSubSemaphore.Release();
				ResetPubSubState();
			}
		}
	}
}