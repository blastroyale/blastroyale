using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class BattlepassRewardDialogPresenter : AnimatedUiPresenterData<BattlepassRewardDialogPresenter.StateData>
	{
		public struct StateData
		{
			public BattlePassRewardConfig RewardConfig;
			public Action ConfirmClicked;
		}

		private Button _confirmButton;

		private void Awake()
		{
			_confirmButton.onClick.AddListener(OnConfirmButtonClicked);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			
		}

		private void OnConfirmButtonClicked()
		{
			Data.ConfirmClicked();
		}
	}
}

