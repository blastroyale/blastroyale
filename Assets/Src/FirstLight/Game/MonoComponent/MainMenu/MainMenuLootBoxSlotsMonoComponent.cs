using System.Threading.Tasks;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This Mono component controls loading and creation of Crates a player has available to unlock the 3D Main Menu scene.
	/// </summary>
	public class MainMenuLootBoxSlotsMonoComponent : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private Transform [] _crateSlotTransforms;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private GameObject[] _boxes;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_services.MessageBrokerService.Subscribe<CratesScreenClosedMessage>(OnCratesScreenClosedMessage);
			_services.MessageBrokerService.Subscribe<CrateClickedMessage>(OnCrateClickedMessage);
			_services.MessageBrokerService.Subscribe<LootBoxOpenedMessage>(OnLootBoxOpenedMessage);
		}

		private async void Start()
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			var mainLootBox = info.MainLootBox;

			_boxes = new GameObject[info.SlotCount];
			
			for (var i = 0; i < info.SlotCount; i++)
			{
				var cacheIndex = i;
				
				if (!info.TimedBoxSlots[i].HasValue)
				{
					continue;
				}
				
				await CreateLootBox(info.TimedBoxSlots[i].Value.Config.LootBoxId, i);
				
				_boxes[cacheIndex].SetActive(!mainLootBox.HasValue || mainLootBox.Value.Data.Slot != cacheIndex);
			}
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.dragging)
			{
				return;
			}

			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();

			if (info.GetSlotsFilledCount() > 0)
			{
				_services.MessageBrokerService.Publish(new MenuWorldLootBoxClickedMessage());
			}
		}

		private void OnCratesScreenClosedMessage(CratesScreenClosedMessage message)
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			var mainLootBox = info.MainLootBox;
			
			for (var i = 0; i < info.SlotCount; i++)
			{
				if (_boxes[i] == null)
				{
					continue;
				}
				
				_boxes[i].SetActive(info.TimedBoxSlots[i].HasValue && (!mainLootBox.HasValue || mainLootBox.Value.Data.Slot != i));
			}
		}

		private void OnLootBoxOpenedMessage(LootBoxOpenedMessage message)
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			
			for (var i = 0; i < info.SlotCount; i++)
			{
				if (_boxes[i] == null)
				{
					continue;
				}

				if (!info.TimedBoxSlots[i].HasValue)
				{
					Destroy(_boxes[i]);
				
					_boxes[i] = null;
				}
			}
		}

		private void OnCrateClickedMessage(CrateClickedMessage message)
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			
			for (var i = 0; i < info.SlotCount; i++)
			{
				if (info.TimedBoxSlots[i].HasValue && info.TimedBoxSlots[i].Value.Data.Id == message.LootBoxId)
				{
					_boxes[i].gameObject.SetActive(false);
				}
				else if (_boxes[i] != null)
				{
					_boxes[i].gameObject.SetActive(true);
				}
			}
		}

		private async Task CreateLootBox(GameId boxId, int index)
		{
			var assetReference = _services.ConfigsProvider.GetConfig<DummyAssetConfigs>().ConfigsDictionary[boxId];
			
			if (!assetReference.OperationHandle.IsValid())
			{
				assetReference.LoadAssetAsync<GameObject>();
			}
			
			if (!assetReference.IsDone)
			{
				await assetReference.OperationHandle.Task;
			}
				
			if (this.IsDestroyed())
			{
				return;
			}
					
			_boxes[index] = Instantiate(assetReference.Asset as GameObject);

			CrateLoaded(_boxes[index], _crateSlotTransforms[index].transform);
		}

		private  void CrateLoaded(GameObject instance, Transform slotTransform)
		{
			var cacheTransform = instance.transform;
			var position = slotTransform.position;

			cacheTransform.SetParent(transform);
			cacheTransform.position = position;
			cacheTransform.rotation = slotTransform.rotation;
			cacheTransform.localScale = slotTransform.localScale;

			instance.SetLayer(gameObject.layer);
		}
	}
}
