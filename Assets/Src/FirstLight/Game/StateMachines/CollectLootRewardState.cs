using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using FirstLight.Game.Messages;
using UnityEngine;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Loot Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CollectLootRewardState
	{
		private readonly IStatechartEvent _collectionCompletedEvent = new StatechartEvent("Collection Completed Event");
		
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameServices _services;
		
		private GameObject _mainCamera;
		private IGameDataProvider _gameDataProvider;
		private List<UniqueId> _lootBoxToOpen = new List<UniqueId>();

		public CollectLootRewardState(IGameServices services, Action<IStatechartEvent> statechartTrigger, IGameDataProvider gameDataProvider)
		{
			_services = services;
			_statechartTrigger = statechartTrigger;
			_gameDataProvider = gameDataProvider;
		}

		/// <summary>
		/// Sets the loot box to open based on the given item <paramref name="uniqueId"/>
		/// </summary>
		public void SetLootBoxToOpen(List<UniqueId> list)
		{
			_lootBoxToOpen = list;
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var loadingState = stateFactory.TaskWait("Loading Collect Loot Scene State");
			var collectingLoot = stateFactory.State("Collecting Loot State");
			var collectingLootFinishedCheck = stateFactory.Choice("Collecting Loot Finished Check");
			var unloadingState = stateFactory.TaskWait("Unloading Collect Loot Scene State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(loadingState);
			initial.OnExit(SubscribeEvents);

			loadingState.WaitingFor(LoadCollectRewardScene).Target(collectingLoot);
			
			collectingLoot.OnEnter(PublishReadyToCollectMessage);
			collectingLoot.Event(_collectionCompletedEvent).Target(collectingLootFinishedCheck);
			
			collectingLootFinishedCheck.Transition().Target(unloadingState);

			unloadingState.WaitingFor(UnloadCollectRewardScene).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<LootBoxCollectedAllMessage>(OnLootBoxCollectedAllMessage);
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private void PublishReadyToCollectMessage()
		{
			_services.MessageBrokerService.Publish(new LootBoxReadyToBeOpenedMessage { ListToOpen = _lootBoxToOpen});
		}

		private async Task LoadCollectRewardScene()
		{
			_mainCamera = Camera.main.gameObject;
			
			_services.AudioFxService.DetachAudioListener();
			_mainCamera.gameObject.SetActive(false);
			
			await _services.AssetResolverService.LoadScene(SceneId.CollectLootRewardSequence, LoadSceneMode.Additive);
		}

		private async Task UnloadCollectRewardScene()
		{
			_services.AssetResolverService.TryGetAssetReference<SceneId, Scene>(SceneId.MainMenu, out var menu);

			await _services.AssetResolverService.UnloadScene(SceneId.CollectLootRewardSequence);

			SceneManager.SetActiveScene(menu.OperationHandle.Convert<SceneInstance>().Result.Scene);
			Resources.UnloadUnusedAssets();

			if (_mainCamera != null)
			{
				_mainCamera.SetActive(true);
				_services.AudioFxService.AudioListener.transform.SetParent(_mainCamera.transform);
			}
		}

		private void OnLootBoxCollectedAllMessage(LootBoxCollectedAllMessage message)
		{
			_statechartTrigger(_collectionCompletedEvent);
		}
	}
}