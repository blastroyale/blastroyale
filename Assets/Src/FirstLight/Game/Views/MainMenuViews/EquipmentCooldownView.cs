using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This view is responsible to set the visual state of an equipment.
	/// It considers the state of being in cooldown that cannot be equipped in the game.
	/// </summary>
	public class EquipmentCooldownView : MonoBehaviour
	{
		private const float TIMER_INTERVAL_SECONDS = 20f;
		
		[SerializeField] private GameObject _visualsBase;
		[SerializeField] private TextMeshProUGUI _cooldownText;
		[SerializeField] private Button _tooltipButton;
		[SerializeField] private Transform _tooltipAnchor;
		
		private UniqueId _uniqueId;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private DateTime _cooldownFinishTime;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		private void OnDisable()
		{
			_tooltipButton.onClick.RemoveAllListeners();
			_services.TickService?.UnsubscribeAllOnUpdate(this);
		}

		/// <summary>
		/// Set's the visual state of the view to be either <paramref name="active"/> or not
		/// </summary>
		public void SetVisualsActive(bool active)
		{
			_visualsBase.SetActive(active);
		}

		/// <summary>
		/// Marks the view to start the visual cooldown countdown 
		/// </summary>
		public void InitCooldown(UniqueId id)
		{
			_uniqueId = id;
			
			var cooldown = _gameDataProvider.EquipmentDataProvider.GetNftInfo(_uniqueId).Cooldown;

			if (cooldown.TotalSeconds > 0)
			{
				SetVisualsActive(true);
				_services.TickService.SubscribeOnUpdate(TickTimerView, TIMER_INTERVAL_SECONDS);
				_tooltipButton.onClick.AddListener(OnTooltipButtonClick);
				_cooldownFinishTime = DateTime.UtcNow + cooldown;
				TickTimerView(0);
			}
			else
			{
				SetVisualsActive(false);
				_services.TickService?.UnsubscribeAllOnUpdate(this);
				_tooltipButton.onClick.RemoveAllListeners();
			}
		}

		private void TickTimerView(float delta)
		{
			var timeDiff = _cooldownFinishTime - DateTime.UtcNow;
			var timeDiffText = timeDiff.ToString(@"h\h\ mm\m");

			if (timeDiff.TotalSeconds > 0)
			{
				_cooldownText.text = string.Format(ScriptLocalization.MainMenu.NftCooldownTimerFullText, timeDiffText);
			}
			else
			{
				SetVisualsActive(false);
				_services.TickService?.UnsubscribeAllOnUpdate(this);
			}
		}

		private void OnTooltipButtonClick()
		{
			_services.GenericDialogService.OpenTooltipDialog(ScriptLocalization.Tooltips.ToolTip_NftCooldown,
			                                                 _tooltipAnchor.position, TooltipArrowPosition.Top);
		}
	}
}
