using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// A button view displaying the current battle pass status.
	/// </summary>
	public class BattlePassButtonView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _progressText;
		[SerializeField] private Image _progressBar;
		[SerializeField] private GameObject _pendingRewardsContainer;
		[SerializeField] private GameObject _progressContainer;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			if (!FeatureFlags.BATTLE_PASS_ENABLED)
			{
				gameObject.SetActive(false);
				return;
			}

			_gameDataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnCurrentPointsUpdated);
		}

		protected void OnDestroy()
		{
			_gameDataProvider.BattlePassDataProvider.CurrentPoints.StopObserving(OnCurrentPointsUpdated);
		}

		private void OnCurrentPointsUpdated(uint previous, uint current)
		{
			var hasRewards = _gameDataProvider.BattlePassDataProvider.IsRedeemable(out var nextLevel);
			_pendingRewardsContainer.SetActive(hasRewards);
			_progressContainer.SetActive(!hasRewards);

			if (!hasRewards)
			{
				_progressText.text = $"{current}/{nextLevel}";
				_progressBar.fillAmount = (float) current / nextLevel;
			}
		}
	}
}