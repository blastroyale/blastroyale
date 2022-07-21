using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Displays information about the player's trophies.
	/// </summary>
	public class TrophiesView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI trophiesText;

		private IGameDataProvider _dataProvider;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();

			OnTrophiesUpdated(0, _dataProvider.PlayerDataProvider.Trophies.Value);
			_dataProvider.PlayerDataProvider.Trophies.Observe(OnTrophiesUpdated);
		}

		private void OnDestroy()
		{
			_dataProvider?.PlayerDataProvider?.Trophies.StopObserving(OnTrophiesUpdated);
		}

		private void OnTrophiesUpdated(uint previous, uint newAmount)
		{
			trophiesText.text = newAmount.ToString();
		}
	}
}