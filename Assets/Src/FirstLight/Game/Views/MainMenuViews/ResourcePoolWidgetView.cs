using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
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
			if (!ShowPool())
			{
				_visualsAnchorRoot.SetActive(false);
				return;
			}

			var poolInfo = _dataProvider.ResourceDataProvider.GetResourcePoolInfo(_poolToObserve);

			if (poolInfo.IsFull)
			{
				_restockText.text = string.Format(ScriptLocalization.MainMenu.ResoucePoolFull);
			}
			else
			{
				var timeDiff = poolInfo.NextRestockTime - DateTime.UtcNow;
				
				_restockText.text = string.Format(ScriptLocalization.MainMenu.ResourceRestockTime, poolInfo.RestockPerInterval,
				                                  timeDiff.ToString(@"h\h\ mm\m"));
			}

			_amountText.text = string.Format(ScriptLocalization.MainMenu.ResourceAmount, poolInfo.CurrentAmount.ToString(), 
			                                 poolInfo.PoolCapacity);
			
			_visualsAnchorRoot.SetActive(true);
		}

		private bool ShowPool()
		{
			return _services.GameModeService.SelectedGameMode.Value.MatchType switch
			{
				MatchType.Custom => false,
				MatchType.Casual => _poolToObserve == GameId.BPP,
				MatchType.Ranked => true,
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}
}