using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Shows the details of a match / event.
	/// </summary>
	public class MatchInfoPopupView : UIView
	{
		private readonly CustomMatchSettings _matchSettings;
		private readonly List<string> _friendsPlaying;

		[Q("Title")] private Label _title;
		[Q("Thumbnail")] private VisualElement _thumbnail;
		[Q("FriendsTitle")] private LocalizedLabel _friendsTitle;
		[Q("FriendsContainer")] private VisualElement _friendsContainer;
		[Q("Summary")] private Label _summary;
		[Q("GameMode")] private MatchSettingsButtonElement _mode;
		[Q("MaxPlayers")] private MatchSettingsButtonElement _maxPlayers;
		[Q("Map")] private MatchSettingsButtonElement _map;
		[Q("SquadSize")] private MatchSettingsButtonElement _teamSize;

		[Q("ActionButton")] private LocalizedButton _actionButton;

		public MatchInfoPopupView(CustomMatchSettings matchSettings, List<string> friendsPlaying)
		{
			_matchSettings = matchSettings;
			_friendsPlaying = friendsPlaying;
		}

		protected override void Attached()
		{
			_mode.SetValue(_matchSettings.GameModeID);
			_mode.SetEnabled(false);
			_teamSize.SetValue(_matchSettings.SquadSize.ToString());
			_teamSize.SetEnabled(false);
			_map.SetValue(_matchSettings.MapID);
			_map.SetEnabled(false);
			_maxPlayers.SetValue(_matchSettings.MaxPlayers.ToString());
			_maxPlayers.SetEnabled(false);

			_friendsTitle.SetVisibility(_friendsPlaying.Count > 0);
			_friendsContainer.Clear();
			foreach (var friend in _friendsPlaying)
			{
				_friendsContainer.Add(new Label(friend));
			}
			
			_thumbnail.RemoveSpriteClasses();
			switch (_matchSettings.SquadSize)
			{
				case 1:
					_thumbnail.AddToClassList("sprite-home__icon-match-solos");
					break;
				case 2:
					_thumbnail.AddToClassList("sprite-home__icon-match-duos");
					break;
				case 4:
					_thumbnail.AddToClassList("sprite-home__icon-match-quads");
					break;
				default:
					throw new NotSupportedException("Unsupported squad size");
			}

			// _mutatorsScroller.Clear();
			// var mutators = _matchSettings.Mutators.GetSetFlags();
			// _mutatorsToggle.value = mutators.Length > 0 || _mutatorsTurnedOn;
			// _mutatorsContainer.EnableInClassList("horizontal-scroll-picker--hidden", !_mutatorsToggle.value); // TODO mihak: I shouldn't have to do this
			//
			// foreach (var mutator in mutators)
			// {
			// 	var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());
			//
			// 	_mutatorsScroller.Add(mutatorLabel);
			// }
			//
			// _filterWeaponsScroller.Clear();
			// var weaponFilter = _matchSettings.WeaponFilter;
			// _filterWeaponsToggle.value = weaponFilter.Count > 0 || _weaponFilterTurnedOn;
			// _filterWeaponsContainer.EnableInClassList("horizontal-scroll-picker--hidden", !_filterWeaponsToggle.value); // TODO mihak: I shouldn't have to do this

			// foreach (var weapon in weaponFilter)
			// {
			// 	_filterWeaponsScroller.Add(new LocalizedLabel(weapon)); // TODO mihak: Add localization key
			// }
			//
			// if (weaponFilter.Count == 0)
			// {
			// 	_filterWeaponsScroller.Add(new LocalizedLabel("All"));
			// }
		}
	}
}