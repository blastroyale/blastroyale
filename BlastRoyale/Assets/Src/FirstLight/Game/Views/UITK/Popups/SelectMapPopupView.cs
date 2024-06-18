using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	public class SelectMapPopupView : UIView
	{
		private readonly Action<string> _onMapSelected;
		private readonly string _gameModeID;
		private readonly string _currentMapID;

		private readonly IGameServices _services;

		public SelectMapPopupView(Action<string> onMapSelected, string gameModeID, string currentMapID)
		{
			_onMapSelected = onMapSelected;
			_gameModeID = gameModeID;
			_currentMapID = currentMapID;

			_services = MainInstaller.ResolveServices();
		}

		protected override void Attached()
		{
			var currentGameMode = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>().First(cfg => cfg.Id == _gameModeID);

			var options = currentGameMode.AllowedMaps
				.Select(id => _services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) id))
				.Where(cfg => !cfg.IsTestMap || Debug.isDebugBuild);

			var mapScroller = Element.Q<ScrollView>("MapScrollView").Required();
			mapScroller.Clear();
			foreach (var mapConfig in options)
			{
				var element = new MatchSettingsSelectionElement(mapConfig.Map.GetLocalizationKey(), mapConfig.Map.GetDescriptionLocalizationKey());
				element.clicked += () => _onMapSelected.Invoke(mapConfig.Map.ToString());

				if (_currentMapID == mapConfig.Map.ToString())
				{
					element.AddToClassList("match-settings-selection--selected");
				}

				mapScroller.Add(element);
				LoadMapPicture(mapConfig, element).Forget();
			}
		}

		private async UniTaskVoid LoadMapPicture(QuantumMapConfig mapConfig, MatchSettingsSelectionElement element)
		{
			var mapImage = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapConfig.Map, false);
			await UniTask.NextFrame(); // Need to wait a frame to make sure the element is attached
			if (element.panel == null) return;
			element.SetImage(mapImage);
		}
	}
}