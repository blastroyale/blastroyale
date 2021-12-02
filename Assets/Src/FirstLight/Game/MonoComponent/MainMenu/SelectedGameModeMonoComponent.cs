using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This View changes the text on the Cinema in the Front End to reflect the currently selected Game Mode.
	/// </summary>
	public class SelectedGameModeMonoComponent : MonoBehaviour
	{
		[SerializeField] private TextMeshPro _gameModeText;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_gameDataProvider.AdventureDataProvider.SelectedMapId.InvokeObserve(OnSelectedAdventureUpdated);
		}

		protected void OnDestroy()
		{
			_gameDataProvider?.AdventureDataProvider?.SelectedMapId?.StopObserving(OnSelectedAdventureUpdated);
		}

		private void OnSelectedAdventureUpdated(int previousValue, int newValue)
		{
			var config = _services.ConfigsProvider.GetConfig<MapConfig>(newValue);
			
			// _gameModeText.SetText(config.Map.GetTranslation()); TODO: Change name when adventure ID changes based on level up / other logic.
		}
		
	}
}
