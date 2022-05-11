using System;
using System.Collections;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
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
		[SerializeField] private GameId _poolToObserve = GameId.CS;
		[SerializeField] private Image _resourceImage;
		[SerializeField] private TextMeshProUGUI _amountText;
		[SerializeField] private TextMeshProUGUI _restockText;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private ResourcePoolConfig? _poolConfig;
		private ResourcePoolData? _currentPoolData;
		private DateTime _nextRestockTime;
		
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
		}

		private void UpdateView()
		{
			_currentPoolData = _dataProvider.CurrencyDataProvider.ResourcePools[_poolToObserve];

			if (!_currentPoolData.HasValue || !_poolConfig.HasValue)
			{
				return;
			}

			_nextRestockTime = _currentPoolData.Value.LastPoolRestockTime.AddMinutes(_poolConfig.Value.RestockIntervalMinutes);

			_amountText.text = string.Format(ScriptLocalization.MainMenu.ResourceAmount,
			                                 _currentPoolData.Value.CurrentResourceAmountInPool.ToString(),
			                                 _poolConfig.Value.PoolCapacity);

			StartCoroutine(RestockTimeCoroutine());
		}

		private IEnumerator RestockTimeCoroutine()
		{
			while (true)
			{
				var timeDiff = _nextRestockTime - DateTime.UtcNow;
				var timeDiffText = timeDiff.ToString(@"h\h\ mm\m");

				_restockText.text = string.Format(ScriptLocalization.MainMenu.ResourceRestockTime,
				                                  _poolConfig.Value.RestockPerInterval, timeDiffText);
				
				var nextTimerUpdateTime = DateTime.UtcNow.AddMinutes(1);
				
				while (DateTime.UtcNow < nextTimerUpdateTime)
				{
					yield return null;
				}

				_nextRestockTime = _nextRestockTime.Subtract(new TimeSpan(0, 1,0));
			}
		}
	}
}