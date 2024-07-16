using System;
using FirstLight.Game.UIElements;
using FirstLight.UIService;
using QuickEye.UIToolkit;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Handles picking between soloc, duos and quads
	/// </summary>
	public class SelectSquadSizePopupView : UIView
	{
		private const string MATCH_SETTINGS_SELECTED = "match-settings-selection--selected";

		[Q("Solos")] private MatchSettingsSelectionElement _solos;
		[Q("Duos")] private MatchSettingsSelectionElement _duos;
		[Q("Quads")] private MatchSettingsSelectionElement _quads;

		private readonly Action<int> _onSquadSizeSelected;
		private readonly uint _currentSize;

		public SelectSquadSizePopupView(Action<int> onSquadSizeSelected, uint currentSize)
		{
			_onSquadSizeSelected = onSquadSizeSelected;
			_currentSize = currentSize;
		}

		protected override void Attached()
		{
			switch (_currentSize)
			{
				case 1:
					_solos.AddToClassList(MATCH_SETTINGS_SELECTED);
					break;
				case 2:
					_duos.AddToClassList(MATCH_SETTINGS_SELECTED);
					break;
				case 4:
					_quads.AddToClassList(MATCH_SETTINGS_SELECTED);
					break;
				default:
					throw new InvalidOperationException($"Invalid squad size: {_currentSize}");
			}

			_solos.clicked += () => _onSquadSizeSelected.Invoke(1);
			_duos.clicked += () => _onSquadSizeSelected.Invoke(2);
			_quads.clicked += () => _onSquadSizeSelected.Invoke(4);
		}
	}
}