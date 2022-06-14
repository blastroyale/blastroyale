using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This view is responsible for displaying relevant resource pool information (current stock, next restock time).
	/// The pool that the widget observes, is handled by _poolToObserve field. Make sure that the pool observed is valid,
	/// and configuration is set up properly on google sheet data, and backend.
	/// </summary>
	public class ResourcePoolWidgetView : MonoBehaviour
	{
		private const float TIMER_INTERVAL_SECONDS = 20f;
		
		[SerializeField] private GameId _poolToObserve = GameId.CS;
		[SerializeField] private Image _resourceImage;
		[SerializeField] private TextMeshProUGUI _amountText;
		[SerializeField] private TextMeshProUGUI _restockText;
		[SerializeField] private GameObject _visualsAnchorRoot;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_services.MessageBrokerService.Subscribe<UpdatedLoadoutMessage>(OnUpdatedLoadoutMessage);
			_services.MessageBrokerService.Subscribe<SelectedGameModeMessage>(OnSelectedGameModeMessage);
		}

		private void OnDestroy()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnEnable()
		{
			_services.TickService.SubscribeOnUpdate(UpdateTimerView, TIMER_INTERVAL_SECONDS);
			UpdateTimerView(0);
		}

		private void OnDisable()
		{
			_services.TickService?.UnsubscribeAllOnUpdate(this);
		}

		private void OnUpdatedLoadoutMessage(UpdatedLoadoutMessage msg)
		{
			UpdateTimerView(0);
		}

		private void OnSelectedGameModeMessage(SelectedGameModeMessage msg)
		{
			UpdateTimerView(0);
		}

		private void UpdateTimerView(float delta)
		{
			if (_dataProvider.AppDataProvider.SelectedGameMode.Value != GameMode.BattleRoyale)
			{
				_visualsAnchorRoot.SetActive(false);
				return;
			}
			
			_visualsAnchorRoot.SetActive(true);
			var currentCapacity = _dataProvider.CurrencyDataProvider.GetCurrentPoolCapacity(_poolToObserve);
			var restockPerInterval = _dataProvider.CurrencyDataProvider.GetPoolRestockAmountPerInterval(_poolToObserve);
			var poolConfig = _services.ConfigsProvider.GetConfig<ResourcePoolConfig>((int) _poolToObserve);
			var currentPoolData = _dataProvider.CurrencyDataProvider.ResourcePools[_poolToObserve];
			var restockForTime = CalculatePoolRestockAmount(poolConfig, currentPoolData, currentCapacity, restockPerInterval) + 1;
			var nextRestockTime = currentPoolData.LastPoolRestockTime.AddMinutes(restockForTime * poolConfig.RestockIntervalMinutes);
			var currentAmount = Math.Clamp(currentPoolData.CurrentResourceAmountInPool, 0, currentCapacity);
			var timeDiff = nextRestockTime - DateTime.UtcNow;
			var timeDiffText = timeDiff.ToString(@"h\h\ mm\m");

			if (currentAmount < currentCapacity)
			{
				_restockText.text = string.Format(ScriptLocalization.MainMenu.ResourceRestockTime, restockPerInterval, timeDiffText);
			}
			else
			{
				_restockText.text = string.Format(ScriptLocalization.MainMenu.ResoucePoolFull);
			}

			_amountText.text = string.Format(ScriptLocalization.MainMenu.ResourceAmount, currentAmount.ToString(), currentCapacity);
		}
		
		private uint CalculatePoolRestockAmount(ResourcePoolConfig config, ResourcePoolData currentPoolData, 
		                                        uint currentCapacity, uint restockPerInterval)
		{
			var minutesElapsedSinceLastRestock = (DateTime.UtcNow - currentPoolData.LastPoolRestockTime).TotalMinutes;
			var amountOfRestocks = (uint) 0;
			
			amountOfRestocks = (uint) Math.Floor(minutesElapsedSinceLastRestock / config.RestockIntervalMinutes);
			
			if (amountOfRestocks == 0)
			{
				return 0;
			}
			
			currentPoolData.CurrentResourceAmountInPool += restockPerInterval * amountOfRestocks;
			
			if (currentPoolData.CurrentResourceAmountInPool > currentCapacity)
			{
				currentPoolData.CurrentResourceAmountInPool = currentCapacity;
			}

			return amountOfRestocks;
		}
	}
}