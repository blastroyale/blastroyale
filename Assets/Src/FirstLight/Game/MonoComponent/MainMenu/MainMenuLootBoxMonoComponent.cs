using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This Mono component controls loading and creation of player character equipment items and skin.
	/// </summary>
	public class MainMenuLootBoxMonoComponent : MonoBehaviour
	{
		[SerializeField] private Transform _frontEndCratesCamera;
		[SerializeField] private TextMeshPro _unlockTimeText;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private GameObject _instance;
		private UniqueId _lootBoxId;
		private Coroutine _coroutineTime;
		private Transform _cameraTransform;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_services.MessageBrokerService.Subscribe<CratesScreenOpenedMessage>(OnCratesScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<CratesScreenClosedMessage>(OnCratesScreenClosedMessage);
			_services.MessageBrokerService.Subscribe<LootBoxUnlockingMessage>(OnLootBoxUnlockingMessage);
			_services.MessageBrokerService.Subscribe<LootBoxOpenedMessage>(OnLootBoxOpenedMessage);
			_services.MessageBrokerService.Subscribe<LootScreenOpenedMessage>(OnLootScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<LootBoxHurryCompletedMessage>(OnLootBoxHurryCompletedMessage);
			_services.MessageBrokerService.Subscribe<CrateClickedMessage>(OnCrateClickedMessage);

			_cameraTransform = Camera.main.transform;
			_unlockTimeText.enabled = false;
		}

		private void Start()
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			
			if (info.MainLootBox.HasValue)
			{
				_lootBoxId = info.MainLootBox.Value.Data.Id;
				
				CreateLootBox();
			}
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void LateUpdate()
		{
			_unlockTimeText.transform.rotation = _cameraTransform.rotation;
		}
		
		private void OnLootBoxHurryCompletedMessage(LootBoxHurryCompletedMessage message)
		{
			if (message.LootBoxId != _lootBoxId)
			{
				return;
			}
			
			if (_coroutineTime != null)
			{
				StopCoroutine(_coroutineTime);
				_coroutineTime = null;
			}
			
			_unlockTimeText.enabled = true;
			_unlockTimeText.text = ScriptLocalization.MainMenu.OpenCrate;
		}

		private void OnPlayScreenOpenedMessage(PlayScreenOpenedMessage message)
		{
			if (_instance != null)
			{
				_instance.SetActive(true);
				_unlockTimeText.enabled = true;
			}
		}

		private void OnLootScreenOpenedMessage(LootScreenOpenedMessage message)
		{
			if (_instance != null)
			{
				_instance.SetActive(false);
			}
			
			_unlockTimeText.enabled = false;
		}

		private void OnCratesScreenOpenedMessage(CratesScreenOpenedMessage callback)
		{
			var cacheTransform = transform;
			var rotation = cacheTransform.rotation.eulerAngles;
			
			cacheTransform.LookAt(_frontEndCratesCamera.position);
			
			rotation.y = cacheTransform.rotation.eulerAngles.y;
			cacheTransform.rotation = Quaternion.Euler(rotation);
			
			if (_instance != null)
			{
				_instance.SetActive(true);
				_unlockTimeText.enabled = true;
			}
		}

		private void OnCratesScreenClosedMessage(CratesScreenClosedMessage message)
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			
			if (info.MainLootBox.HasValue)
			{
				_lootBoxId = info.MainLootBox.Value.Data.Id;
				
				CreateLootBox();

				return;
			}
			
			_lootBoxId = UniqueId.Invalid;
			
			CleanCrate();
		}
		
		private void OnLootBoxUnlockingMessage(LootBoxUnlockingMessage callback)
		{
			_lootBoxId = callback.LootBoxId;
			
			CreateLootBox();
		}

		private void OnLootBoxOpenedMessage(LootBoxOpenedMessage callback)
		{
			if (callback.LootBoxId != _lootBoxId)
			{
				return;
			}

			CleanCrate();
		}

		private void OnCrateClickedMessage(CrateClickedMessage message)
		{
			if (message.LootBoxId == _lootBoxId)
			{
				return;
			}
				
			_lootBoxId = message.LootBoxId;
			
			CreateLootBox();
		}
		
		private void CreateLootBox()
		{
			var info = _gameDataProvider.LootBoxDataProvider.GetTimedBoxInfo(_lootBoxId);
			var boxState = info.GetState(_services.TimeService.DateTimeUtcNow);
			
			CleanCrate();

			_unlockTimeText.enabled = true;
				
			if (boxState == LootBoxState.Unlocked)
			{
				_unlockTimeText.text = ScriptLocalization.MainMenu.OpenCrate;
			}
			else if (boxState == LootBoxState.Locked)
			{
				_unlockTimeText.text = ScriptLocalization.MainMenu.Unlock;
			}
			else
			{
				_unlockTimeText.text = "";
				_coroutineTime = StartCoroutine(UpdateState(info.Data.EndTime));
			}
			
			_services.AssetResolverService.RequestAsset<GameId, GameObject>(info.Config.LootBoxId, true, true, CrateLoaded);
		}
		
		private IEnumerator UpdateState(DateTime entTime)
		{
			var time = entTime - _services.TimeService.DateTimeUtcNow;
			var waiter = new WaitForSeconds(1);

			while (time.TotalSeconds > 0)
			{
				_unlockTimeText.text = ((uint) time.TotalSeconds).ToHoursMinutesSeconds();
				
				yield return waiter;
				
				time = entTime - _services.TimeService.DateTimeUtcNow;
			}

			_unlockTimeText.text = ScriptLocalization.MainMenu.OpenCrate;
			_coroutineTime = null;
		}
		
		private void CrateLoaded(GameId id, GameObject instance, bool instantiated)
		{
			var cacheTransform = instance.transform;
			
			cacheTransform.SetParent(transform);
			
			_instance = instance;
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;
			cacheTransform.localScale = Vector3.one;

			instance.SetLayer(gameObject.layer);
		}

		private void CleanCrate()
		{
			if(_instance != null)
			{
				Destroy(_instance);
				_instance = null;
			}
			
			if (_coroutineTime != null)
			{
				StopCoroutine(_coroutineTime);
				_coroutineTime = null;
			}
			
			_unlockTimeText.enabled = false;
		}
	}
}
