using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Signals;
using FirstLight.Game.TimelinePlayables;
using FirstLight.Game.Utils;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class handles the Timeline sequence for when a player open a loot box and collects the rewards from within it.
	/// </summary>
	public class CollectLootScreenView : MonoBehaviour
	{
		private const float _tweenDelayInc = 0.1f;
		
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private GameObject _summaryHolder;
		[SerializeField, Required] private Transform _targetGrid;
		[SerializeField, Required] private Transform _lootBoxTransform;
		[SerializeField, Required] private Transform _instantiateTransform;
		[SerializeField, Required] private Button _startSequenceButton;
		[SerializeField, Required] private Button _leaveButton;
		[SerializeField, Required] private Button _tapToSkipButton;
		[SerializeField, Required] private PlayableDirector _timelineBox;
		[SerializeField, Required] private PlayableDirector _timelineCore;
		[SerializeField, Required] private CollectedLootView _lootCardRef;

		private PlayableDirector _director;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private GameObject _lootItem;
		private GameObject _lootBox;
		private bool _openedLootBoxCommandExecuted;
		private IObjectPool<CollectedLootView> _lootCardViewPool;
		private List<EquipmentDataInfo> _loot = new List<EquipmentDataInfo>();
		private List<UniqueId> _lootBoxToOpen = new List<UniqueId>();
		private GameId _currentBoxGameId;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_lootCardViewPool = new GameObjectPool<CollectedLootView>(10, _lootCardRef);

			Reset();
			
			_startSequenceButton.onClick.AddListener(StartSequenceClicked);
			_leaveButton.onClick.AddListener(LeaveButtonClicked);
			
			_services.MessageBrokerService.Subscribe<LootBoxReadyToBeOpenedMessage>(OnLootBoxReadyToBeOpenedMessage);
			_services.MessageBrokerService.Subscribe<LootBoxOpenedMessage>(OnLootBoxRewardCollectedMessage);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void Reset()
		{
			_openedLootBoxCommandExecuted = false;
			_lootCardRef.gameObject.SetActive(false);
			_summaryHolder.SetActive(false);
			_leaveButton.gameObject.SetActive(false);
			_tapToSkipButton.gameObject.SetActive(false);
			_startSequenceButton.gameObject.SetActive(false);
			_titleText.gameObject.SetActive(false);
		}

		private void OnLootBoxReadyToBeOpenedMessage(LootBoxReadyToBeOpenedMessage message)
		{
			_lootBoxToOpen = new List<UniqueId>(message.ListToOpen);
			
			StartSequence();
		}

		private async void StartSequence()
		{
			var gameId = _gameDataProvider.UniqueIdDataProvider.Ids[_lootBoxToOpen[0]];
			_currentBoxGameId = gameId;
			
			_loot = _gameDataProvider.LootBoxDataProvider.Peek(_lootBoxToOpen[0]);
			
			_startSequenceButton.gameObject.SetActive(false);
			_summaryHolder.SetActive(false);
			_leaveButton.gameObject.SetActive(false);
			_tapToSkipButton.gameObject.SetActive(false);
			_lootCardViewPool.DespawnAll();

			if (_lootBox != null)
			{
				Destroy(_lootBox);
			}
			
			_lootBox = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(gameId);
			
			if (this.IsDestroyed())
			{
				return;
			}

			_director = _timelineBox;
			
			var goTransform = _lootBox.transform;
			
			_lootBox.SetLayer(gameObject.layer);
			
			goTransform.SetParent(_lootBoxTransform);
			goTransform.localScale = Vector3.one;
			goTransform.localPosition = Vector3.zero;
			
			if (gameId.IsInGroup(GameIdGroup.CoreBox))
			{
				_director = _timelineCore;
				goTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
			}
			
			foreach (var playableAssetOutput in _director.playableAsset.outputs)
			{
				if (playableAssetOutput.streamName == "LootBoxAnimator")
				{
					_director.SetGenericBinding(playableAssetOutput.sourceObject, _lootBox.GetComponent<Animator>());
				}
				else if ( playableAssetOutput.streamName == "LootBox" || playableAssetOutput.streamName == "LootCore" )
				{
					_director.SetGenericBinding(playableAssetOutput.sourceObject, _lootBox);
				}
			}
			
			_director.time = 0;
			_director.Play();
		}

		private void SetTitleText()
		{
			_titleText.gameObject.SetActive(true);
			
			string crateText;

			if (_currentBoxGameId.IsInGroup(GameIdGroup.TimeBox))
			{
				var info = _gameDataProvider.LootBoxDataProvider.GetTimedBoxInfo(_lootBoxToOpen[0].Id);
				crateText = info.Config.Tier.ToString();
			}
			else
			{
				var info = _gameDataProvider.LootBoxDataProvider.GetCoreBoxInfo(_lootBoxToOpen[0].Id);
				crateText = info.Config.Tier.ToString();
			}

			var showText = string.Format(ScriptLocalization.MainMenu.CrateTierType, _currentBoxGameId.GetTranslation(), crateText);
			_titleText.text = showText;
		}
		
		/// <inheritdoc />
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			if (notification is DirectorPauseSignal)
			{
				SetTitleText();
				_startSequenceButton.gameObject.SetActive(true);
				_director.Pause();
				return;
			}

			if (notification is DirectorCompleteSignal && !_openedLootBoxCommandExecuted)
			{
				_openedLootBoxCommandExecuted = true;
				_services.CommandService.ExecuteCommand(new OpenLootBoxCommand{LootBoxId = _lootBoxToOpen[0]});
				_summaryHolder.SetActive(true);
				return;
			}

			var destinationMaker = notification as DestinationMarker;
			if (destinationMaker != null )
			{
				Destroy(_lootItem);
				
				_lootItem = null;
				
				_services.AudioFxService.PlayClip2D(AudioId.CollectPickupSpecial);
				
				RevealLootItem();
			}
			
			var jumpMaker = notification as JumpMarker;
			if (jumpMaker != null && _loot.Count > 0)
			{
				_director.playableGraph.GetRootPlayable(0).SetTime(jumpMaker.destinationMarker.time);
			}
		}

		/// <summary>
		/// All 3D Animations have finished. Show the 2D UI Rackup.
		/// </summary>
		private void OnLootBoxRewardCollectedMessage(LootBoxOpenedMessage message)
		{
			for (var i = 0; i < message.LootBoxContent.Count; i++)
			{
				var lootView = _lootCardViewPool.Spawn();
				lootView.SetInfo(message.LootBoxContent[i]);
				lootView.gameObject.SetActive(true);
				lootView.transform.SetParent(_targetGrid);
				lootView.PlayCollectedTween(_tweenDelayInc * i, OnTweenCompleted);
			}
		}

		private void OnTweenCompleted()
		{
			_leaveButton.gameObject.SetActive(true);
		}
		
		private void StartSequenceClicked()
		{
			_tapToSkipButton.onClick.AddListener(OnSkipButtonClicked);
			_startSequenceButton.gameObject.SetActive(false);
			_tapToSkipButton.gameObject.SetActive(true);
			_titleText.gameObject.SetActive(false);
			_director.Resume();
		}

		private void LeaveButtonClicked()
		{
			_lootBoxToOpen.RemoveAt(0);

			// If we have more boxes to process, we need to restart the sequence with the next box in the list.
			if (_lootBoxToOpen.Count > 0)
			{
				Reset();
				StartSequence();

				return;
			}
			
			_services.MessageBrokerService.Publish(new LootBoxCollectedAllMessage());
			_tapToSkipButton.onClick.RemoveListener(OnSkipButtonClicked);
		}

		private void OnSkipButtonClicked()
		{
			if (_loot.Count == 0 || _director.state != PlayState.Playing || _lootItem == null)
			{
				return;
			}
			
			Destroy(_lootItem);
			_titleText.text = "";
				
			var timeline = (TimelineAsset) _director.playableAsset;
			var jumpMarker = (JumpMarker)timeline.markerTrack.GetMarker(1);
			
			_director.playableGraph.GetRootPlayable(0).SetTime(jumpMarker.destinationMarker.time);
		}

		private async void RevealLootItem()
		{
			if (_lootBox != null)
			{
				Destroy(_lootBox);
			}
			
			var item = _loot.ElementAt(0);
			
			_loot.RemoveAt(0);

			_lootItem = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(item.GameId);

			if (this.IsDestroyed())
			{
				return;
			}
			
			var goTransform = _lootItem.transform;
		
			_lootItem.SetLayer(gameObject.layer);
			goTransform.SetParent(_instantiateTransform);
			
			goTransform.localScale = Vector3.one;
			goTransform.localPosition = Vector3.zero;
			goTransform.localRotation = Quaternion.identity;
			
			_titleText.text = item.GameId.GetTranslation();
		}
	}
}
