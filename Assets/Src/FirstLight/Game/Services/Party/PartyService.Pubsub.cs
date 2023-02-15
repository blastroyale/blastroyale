using System;
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
			foreach (var change in obj.LobbyChanges)
			{
				if (_lobby.ChangeNumber < change.ChangeNumber)
				{
#pragma warning disable CS4014
					RefetchCachedParty();
#pragma warning restore CS4014
					break;
				}
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