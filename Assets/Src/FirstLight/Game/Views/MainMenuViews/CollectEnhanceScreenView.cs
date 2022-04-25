using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Signals;
using FirstLight.Game.TimelinePlayables;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class handles the Timeline sequence for when a player fuses or enhances an item.
	/// </summary>
	public class CollectEnhanceScreenView : MonoBehaviour, INotificationReceiver
	{
		private const float _tweenDelayInc = 0.1f;
		
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private GameObject _summaryHolder;
		[SerializeField, Required] private Button _leaveButton;
		[SerializeField, Required] private Button _tapToSkipButton;
		[SerializeField, Required] private PlayableDirector _timeline;
		[SerializeField, Required] private CollectedLootView _lootCardRef;
		[SerializeField, Required] private Transform _targetGrid;
		[SerializeField] private Transform[] _enhanceTransforms;
		[SerializeField, Required] private Transform _newItemTransform;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private GameObject _enhancedItemObject;
		private GameObject [] _enhanceMaterials;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			Reset();
			
			_leaveButton.onClick.AddListener(LeaveButtonClicked);
			_tapToSkipButton.onClick.AddListener(OnSkipButtonClicked);
			
			_services.MessageBrokerService.Subscribe<EnhanceSequenceReadyMessage>(OnStartEnhanceSequenceMessage);
			_services.MessageBrokerService.Subscribe<ItemsEnhancedMessage>(OnItemEnhancedMessage);
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

		private async void OnItemEnhancedMessage(ItemsEnhancedMessage message)
		{
			var enhancedGameId = message.ResultItem.GameId;
			
			_lootCardRef.SetInfo(message.ResultItem);
			
			_enhancedItemObject = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(enhancedGameId);
			_enhancedItemObject.SetLayer(gameObject.layer);
			_enhancedItemObject.transform.SetParent(_newItemTransform);
			_enhancedItemObject.transform.localPosition = Vector3.zero;
			_enhancedItemObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
			_enhancedItemObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		}

		private async void OnStartEnhanceSequenceMessage(EnhanceSequenceReadyMessage message)
		{
			var items = message.EnhanceList;
			
			_summaryHolder.SetActive(false);

			_enhanceMaterials = new GameObject[items.Count];
			
			for (var i = 0; i <  items.Count; i++)
			{
				_enhanceMaterials[i] = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(_gameDataProvider.UniqueIdDataProvider.Ids[items[i]]);
				
				_enhanceMaterials[i].transform.SetParent(_enhanceTransforms[i]);
				_enhanceMaterials[i].SetLayer(gameObject.layer);
				_enhanceMaterials[i].transform.localPosition = Vector3.zero;
			}

			_timeline.time = 0;
			_timeline.Play();
			
			_services.CommandService.ExecuteCommand(new EnhanceItemsCommand { EnhanceItems = items });
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
			_enhancedItemObject.SetActive(false);
			_leaveButton.gameObject.SetActive(true);
		}
		
		private void LeaveButtonClicked()
		{
			_services.MessageBrokerService.Publish(new EnhanceCompletedMessage());
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
			if (!_summaryHolder.activeSelf)
			{
				_enhancedItemObject.SetActive(false);

				foreach (var item in _enhanceMaterials)
				{
					item?.SetActive(false);
				}
				
				_summaryHolder.SetActive(true);
				_titleText.enabled = true;
				_lootCardRef.gameObject.SetActive(true);
				_lootCardRef.transform.SetParent(_targetGrid);
				_lootCardRef.PlayCollectedTween(_tweenDelayInc, OnTweenCompleted);
			}
		}
	}
}
