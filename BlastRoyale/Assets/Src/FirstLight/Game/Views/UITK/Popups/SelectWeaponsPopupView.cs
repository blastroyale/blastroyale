using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Shows a list of all available weapons and allows the user to select as many as they want.
	/// </summary>
	public class SelectWeaponsPopupView : UIView
	{
		[Q("WeaponsScrollView")] private ScrollView _scroller;
		[Q("SelectedWeaponsLabel")] private Label _selectedLabel;
		[Q("ConfirmButton")] private LocalizedButton _confirmButton;

		private readonly IGameServices _services;

		private readonly Action<List<string>> _onWeaponsSelected;
		private readonly List<string> _currentWeapons;

		public SelectWeaponsPopupView(Action<List<string>> onWeaponsSelected, List<string> currentWeapons)
		{
			_services = MainInstaller.ResolveServices();

			_onWeaponsSelected = onWeaponsSelected;
			_currentWeapons = currentWeapons;
		}

		protected override void Attached()
		{
			var options = GameIdGroup.Weapon.GetIds().ToArray();

			_scroller.Clear();
			foreach (var weapon in options)
			{
				var element = new MatchSettingsSelectionElement(weapon.GetLocalizationKey(), string.Empty);
				element.userData = weapon;
				element.clicked += () => OnWeaponClicked(element);

				if (_currentWeapons.Contains(weapon.ToString()))
				{
					element.AddToClassList("match-settings-selection--selected");
				}

				_scroller.Add(element);

				LoadWeaponPicture(weapon, element).Forget();
			}

			_confirmButton.Required().clicked += OnConfirmClicked;
			_selectedLabel.text = string.Format(ScriptLocalization.UITCustomGames.selected_weapons, GetSelectedWeapons().Count);
		}

		private void OnWeaponClicked(MatchSettingsSelectionElement element)
		{
			element.ToggleInClassList("match-settings-selection--selected");
			_selectedLabel.text = string.Format(ScriptLocalization.UITCustomGames.selected_weapons, GetSelectedWeapons().Count);
		}

		private void OnConfirmClicked()
		{
			_onWeaponsSelected(GetSelectedWeapons());
		}

		private List<string> GetSelectedWeapons()
		{
			return _scroller.Children()
				.Where(ve => ve.ClassListContains("match-settings-selection--selected"))
				.Select(ve => ((GameId) ve.userData).ToString())
				.ToList();
		}

		private async UniTaskVoid LoadWeaponPicture(GameId weapon, MatchSettingsSelectionElement element)
		{
			var mapImage = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weapon);
			if (element.panel == null) return;
			FLog.Info("PACO", "WeaponLoaded");
			element.SetImage(mapImage);
		}
	}
}