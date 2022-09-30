using System;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class BattlepassRewardDialogPresenter : AnimatedUiPresenterData<BattlepassRewardDialogPresenter.StateData>
	{
		public struct StateData
		{
			public Equipment Reward;
			public Action ConfirmClicked;
		}

		private IGameServices _services;
		
		[SerializeField, Required] private Button _confirmButton;
		[SerializeField, Required] private EquipmentCardView _rewardCard;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_confirmButton.onClick.AddListener(OnConfirmButtonClicked);
		}

		protected override async void OnOpened()
		{
			base.OnOpened();
			
			_rewardCard.gameObject.SetActive(false);
			await _rewardCard.Initialise(Data.Reward);
			_rewardCard.gameObject.SetActive(true);
		}

		private void OnConfirmButtonClicked()
		{
			Data.ConfirmClicked();
		}
	}
}

