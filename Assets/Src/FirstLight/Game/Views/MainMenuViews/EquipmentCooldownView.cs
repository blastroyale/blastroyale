using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
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
	public class EquipmentCooldownView : MonoBehaviour
	{

		[SerializeField] private TextMeshProUGUI _cooldownText;
		[SerializeField] private Button _tooltipButton;
		
		private const float TIMER_INTERVAL_SECONDS = 20f;
		
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
			_services.TickService?.UnsubscribeAllOnUpdate(this);
		}

		public void InitCooldown(UniqueId id)
		{
			_uniqueId = id;
			
			TimeSpan cooldown = _gameDataProvider.EquipmentDataProvider.GetItemCooldown(_uniqueId);
			
			if (cooldown.TotalSeconds > 0)
			{
				_services.TickService.SubscribeOnUpdate(TickTimerView, TIMER_INTERVAL_SECONDS);
				_cooldownFinishTime = DateTime.UtcNow + cooldown;
				TickTimerView(0);
			}
			else
			{
				_services.TickService?.UnsubscribeAllOnUpdate(this);
				gameObject.SetActive(false);
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
				_services.TickService?.UnsubscribeAllOnUpdate(this);
				gameObject.SetActive(false);
			}
		}
	}
}
