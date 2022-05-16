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
	/// <summary>
	/// This view is responsible for displaying relevant resource pool information (current stock, next restock time).
	/// The pool that the widget observes, is handled by _poolToObserve field. Make sure that the pool observed is valid,
	/// and configuration is set up properly on google sheet data, and backend.
	/// </summary>
	public class ResourcePoolWidgetView : MonoBehaviour
	{
		private const float TIMER_INTERVAL_SECONDS = 1f;
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

			_poolConfig = _services.ConfigsProvider.GetConfig<ResourcePoolConfig>((int) _poolToObserve);
		}

		private void OnEnable()
		{
			_services.TickService.SubscribeOnUpdate(TickTimerView, TIMER_INTERVAL_SECONDS);
		}

		private void OnDisable()
		{
			_services.TickService?.UnsubscribeAllOnUpdate(this);
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void TickTimerView(float delta)
		{
			_currentPoolData = _dataProvider.CurrencyDataProvider.ResourcePools[_poolToObserve];

			uint restockForTime = _currentPoolData.RestockWithoutTimeUpdate(_poolConfig) + 1;

			_nextRestockTime = _currentPoolData.LastPoolRestockTime.AddMinutes(restockForTime * _poolConfig.RestockIntervalMinutes);
			_currentAmount = _currentPoolData.CurrentResourceAmountInPool;

			Debug.LogError(restockForTime + " | LastRestock: " + _currentPoolData.LastPoolRestockTime +
			               "   NextRestock: " + _nextRestockTime);

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
		}
	}
}