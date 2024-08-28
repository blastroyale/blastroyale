using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Shows a list of all available maps and allows the user to select one.
	/// </summary>
	public class SelectMapPopupView : UIView
	{
		[Q("MapScrollView")] private ScrollView _mapScrollView;

		private readonly Action<string> _onMapSelected;
		private readonly List<GameId> _options;
		private readonly string _currentMapID;

		private readonly IGameServices _services;

		public SelectMapPopupView(Action<string> onMapSelected, IEnumerable<GameId> options, string currentMapID, bool addAny)
		{
			_onMapSelected = onMapSelected;
			_options = options.ToList();
			_currentMapID = currentMapID;

			if (addAny)
			{
				_options.Insert(0, GameId.Any);
			}

			_services = MainInstaller.ResolveServices();
		}

		protected override void Attached()
		{
			var mapScroller = _mapScrollView.Required();
			mapScroller.Clear();
			foreach (var mapConfig in _options)
			{
				var element = new MatchSettingsSelectionElement(mapConfig.GetLocalizationKey(), mapConfig.GetDescriptionLocalizationKey());
				element.clicked += () => _onMapSelected.Invoke(mapConfig.ToString());

				if (_currentMapID == mapConfig.ToString())
				{
					element.AddToClassList("match-settings-selection--selected");
				}

				mapScroller.Add(element);
				LoadMapPicture(mapConfig, element).Forget();
			}
		}

		private async UniTaskVoid LoadMapPicture(GameId mapID, MatchSettingsSelectionElement element)
		{
			var mapImage = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(mapID, false);
			await UniTask.NextFrame(); // Need to wait a frame to make sure the element is attached
			if (element.panel == null) return;
			element.SetImage(mapImage);
		}
	}
}