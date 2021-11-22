using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
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
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class handles the Timeline sequence for when a player fuses an item.
	/// </summary>
	public class CollectFuseScreenView : MonoBehaviour, INotificationReceiver
	{
		private const float _tweenDelayInc = 0.1f;
		
		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private GameObject _summaryHolder;
		[SerializeField] private Button _leaveButton;
		[SerializeField] private Button _tapToSkipButton;
		[SerializeField] private PlayableDirector _timeline;
		[SerializeField] private CollectedLootView _lootCardRef;
		[SerializeField] private Transform _targetGrid;
		[SerializeField] private Transform[] _fusionTransforms;
		[SerializeField] private Transform _newItemTransform;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private GameObject _fusedItemObject;
		private GameObject [] _fusionMaterials;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_fusionMaterials = new GameObject[5];

			Reset();
			
			_leaveButton.onClick.AddListener(LeaveButtonClicked);
			_tapToSkipButton.onClick.AddListener(OnSkipButtonClicked);
			
			_services.MessageBrokerService.Subscribe<FuseSequenceReadyMessage>(OnStartFuseSequenceMessage);
			_services.MessageBrokerService.Subscribe<ItemsFusedMessage>(OnItemFusedMessage);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void Reset()
		{
			_lootCardRef.gameObject.SetActive(false);
			_summaryHolder.SetActive(false);
			_leaveButton.gameObject.SetActive(false);
			_titleText.enabled = false;
		}

		private async void OnItemFusedMessage(ItemsFusedMessage message)
		{
			_lootCardRef.SetInfo(message.ResultItem);
			
			_fusedItemObject = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(message.ResultItem.GameId);
			_fusedItemObject.SetLayer(gameObject.layer);
			_fusedItemObject.transform.SetParent(_newItemTransform);
			_fusedItemObject.transform.localPosition = Vector3.zero;
			_fusedItemObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
			_fusedItemObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		}

		private async void OnStartFuseSequenceMessage(FuseSequenceReadyMessage message)
		{
			var items = message.FusionList;
			
			_summaryHolder.SetActive(false);

			for (var i = 0; i <  items.Count; i++)
			{
				_fusionMaterials[i] = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(_gameDataProvider.UniqueIdDataProvider.Ids[items[i]]);
				
				_fusionMaterials[i].transform.SetParent(_fusionTransforms[i]);
				_fusionMaterials[i].SetLayer(gameObject.layer);
				_fusionMaterials[i].transform.localPosition = Vector3.zero;
			}

			_timeline.time = 0;
			
			_timeline.Play();
			_services.CommandService.ExecuteCommand(new FuseCommand { FusingItems = items });
		}
		
		
		/// <inheritdoc />
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			if (notification is DirectorPauseSignal)
			{
				return;
			}

			if (notification is DirectorCompleteSignal)
			{
				if (!_summaryHolder.activeSelf)
				{
					_tapToSkipButton.gameObject.SetActive(false);
					
				     this.LateCall(1, DirectorComplete);
					
				}
				
				return;
			}

			var destinationMaker = notification as DestinationMarker;
			if (destinationMaker != null )
			{
				_services.AudioFxService.PlayClip2D(AudioId.CollectPickupSpecial);
			}
		}

		private void OnTweenCompleted()
		{
			_fusedItemObject.SetActive(false);
			_leaveButton.gameObject.SetActive(true);
		}
		
		private void LeaveButtonClicked()
		{
			_services.MessageBrokerService.Publish(new FuseCompletedMessage());
		}

		private void OnSkipButtonClicked()
		{
			if (!_summaryHolder.activeSelf || _timeline.state != PlayState.Playing)
			{
				return;
			}
			
			_timeline.Stop();
			DirectorComplete();
		}

		private void DirectorComplete()
		{
			if (_summaryHolder.activeSelf)
			{
				return;
			}
			
			_fusedItemObject.SetActive(false);

			foreach (var item in _fusionMaterials)
			{
				item.SetActive(false);
			}
				
			_summaryHolder.SetActive(true);
			_titleText.enabled = true;
			_lootCardRef.gameObject.SetActive(true);
			_lootCardRef.transform.SetParent(_targetGrid);
			_lootCardRef.PlayCollectedTween(_tweenDelayInc, OnTweenCompleted);
		}
	}
}
