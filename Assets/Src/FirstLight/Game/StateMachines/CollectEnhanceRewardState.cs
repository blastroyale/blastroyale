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
	/// This object contains the behaviour logic for the Collect Fused Item State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class CollectEnhanceRewardState
	{
		private readonly IStatechartEvent _collectionCompletedEvent = new StatechartEvent("Collection Completed Event");
		
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameServices _services;
		
		private GameObject _mainCamera;
		private IGameDataProvider _gameDataProvider;
		private List<UniqueId> _enhanceList = new List<UniqueId>();

		public CollectEnhanceRewardState(IGameServices services, Action<IStatechartEvent> statechartTrigger, IGameDataProvider gameDataProvider)
		{
			_services = services;
			_statechartTrigger = statechartTrigger;
			_gameDataProvider = gameDataProvider;
		}

		/// <summary>
		/// Sets the list of items to Fuse / Enhance <paramref name="uniqueId"/>
		/// </summary>
		public void SetEnhanceList(List<UniqueId> list)
		{
			_enhanceList = list;
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var loadingState = stateFactory.TaskWait("Loading Collect Enhance Scene State");
			var collectingEnhance = stateFactory.State("Collecting Enhance State");
			var collectingEnhanceFinishedCheck = stateFactory.Choice("Collecting Enhance Finished Check");
			var unloadingState = stateFactory.TaskWait("Unloading Collect Enhance Scene State");
			var final = stateFactory.Final("Final");

			initial.Transition().Target(loadingState);
			initial.OnExit(SubscribeEvents);

			loadingState.WaitingFor(LoadEnhanceScene).Target(collectingEnhance);
			
			collectingEnhance.OnEnter(PublishStartEnhanceSequenceMessage);
			collectingEnhance.Event(_collectionCompletedEvent).Target(collectingEnhanceFinishedCheck);
			
			collectingEnhanceFinishedCheck.Transition().Target(unloadingState);

			unloadingState.WaitingFor(UnloadCollectRewardScene).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<EnhanceCompletedMessage>(OnEnhanceCompletedMessage);
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}
		
		
		private void PublishStartEnhanceSequenceMessage()
		{
			_services.MessageBrokerService.Publish(new EnhanceSequenceReadyMessage(){EnhanceList = _enhanceList} );
		}

		private async Task LoadEnhanceScene()
		{
			_mainCamera = Camera.main.gameObject;
			
			_services.AudioFxService.DetachAudioListener();
			_mainCamera.gameObject.SetActive(false);
			
			await _services.AssetResolverService.LoadScene(SceneId.EnhanceSequence, LoadSceneMode.Additive);
		}

		private async Task UnloadCollectRewardScene()
		{
			_services.AssetResolverService.TryGetAssetReference<SceneId, Scene>(SceneId.MainMenu, out var menu);

			await _services.AssetResolverService.UnloadScene(SceneId.EnhanceSequence);

			SceneManager.SetActiveScene(menu.OperationHandle.Convert<SceneInstance>().Result.Scene);
			Resources.UnloadUnusedAssets();

			if (_mainCamera != null)
			{
				_mainCamera.SetActive(true);
				_services.AudioFxService.AudioListener.transform.SetParent(_mainCamera.transform);
			}
		}

		private void OnEnhanceCompletedMessage(EnhanceCompletedMessage message)
		{
			_statechartTrigger(_collectionCompletedEvent);
		}
	}
}