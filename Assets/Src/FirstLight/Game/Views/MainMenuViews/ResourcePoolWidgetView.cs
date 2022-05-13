using System;
using System.Collections;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	public class ResourcePoolWidgetView : MonoBehaviour
	{
		private const int TIMER_INTERVAL_SECONDS = 20;
		[SerializeField] private GameId _poolToObserve = GameId.CS;
		[SerializeField] private Image _resourceImage;
		[SerializeField] private TextMeshProUGUI _amountText;
		[SerializeField] private TextMeshProUGUI _restockText;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private ResourcePoolConfig _poolConfig;
		private ResourcePoolData _currentPoolData;
		private DateTime _nextRestockTime;
		private ulong _currentAmount;
		
		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_poolConfig = _services.ConfigsProvider.GetConfigsList<ResourcePoolConfig>()
			                       .FirstOrDefault(x => x.Id == _poolToObserve);
		}

		private void OnEnable()
		{
			UpdateView();
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage msg)
		{
			StopAllCoroutines();
			UpdateView();
		}

		private void OnDisable()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void UpdateView()
		{
			StartCoroutine(ViewUpdateCoroutine());
		}

		private IEnumerator ViewUpdateCoroutine()
		{
			while (true)
			{
				_currentPoolData = _dataProvider.CurrencyDataProvider.ResourcePools[_poolToObserve];
				
				uint restockForTime = _currentPoolData.RestockWithoutTimeUpdate(_poolConfig) + 1;

				yield return 
				
				_nextRestockTime = _currentPoolData.LastPoolRestockTime.AddMinutes(restockForTime * _poolConfig.RestockIntervalMinutes);
				_currentAmount = _currentPoolData.CurrentResourceAmountInPool;
				
				Debug.LogError(restockForTime + " | LastRestock: " + _currentPoolData.LastPoolRestockTime + "   NextRestock: " + _nextRestockTime);
				
				var timeDiff = _nextRestockTime - DateTime.UtcNow;
				var timeDiffText = timeDiff.ToString(@"h\h\ mm\m");

				if (_currentAmount < _poolConfig.PoolCapacity)
				{
					_restockText.text = string.Format(ScriptLocalization.MainMenu.ResourceRestockTime,
					                                  _poolConfig.RestockPerInterval, timeDiffText);
				}
				else
				{
					_restockText.text = string.Format(ScriptLocalization.MainMenu.ResoucePoolFull);
				}

				_amountText.text = string.Format(ScriptLocalization.MainMenu.ResourceAmount,
				                                 _currentAmount.ToString(),
				                                 _poolConfig.PoolCapacity);
				
				var nextTimerUpdateTime = DateTime.UtcNow.AddSeconds(TIMER_INTERVAL_SECONDS);

				if (_nextRestockTime < nextTimerUpdateTime && _nextRestockTime > DateTime.UtcNow)
				{
					Debug.LogError("TIME CORRECTION - SHORTER TIMER UPDATE");
					nextTimerUpdateTime = _nextRestockTime;
				}

				while (DateTime.UtcNow < nextTimerUpdateTime)
				{
					yield return null;
				}
			}
		}
	}
}