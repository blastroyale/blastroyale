using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
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
		[SerializeField, Required] private Image _rewardImage;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_confirmButton.onClick.AddListener(OnConfirmButtonClicked);
		}

		protected override async void OnOpened()
		{
			base.OnOpened();
			
			_rewardImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(Data.Reward.GameId);
		}

		private void OnConfirmButtonClicked()
		{
			Data.ConfirmClicked();
		}
	}
}

