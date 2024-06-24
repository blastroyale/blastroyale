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
		[Q("Solos")] private MatchSettingsSelectionElement  _solos;
		[Q("Duos")] private MatchSettingsSelectionElement  _duos;
		[Q("Quads")] private MatchSettingsSelectionElement  _quads;
		
		private readonly Action<int> _onSquadSizeSelected;
		private readonly int _currentSize;

		public SelectSquadSizePopupView(Action<int> onSquadSizeSelected, int currentSize)
		{
			_onSquadSizeSelected = onSquadSizeSelected;
			_currentSize = currentSize;
		}

		protected override void Attached()
		{
			switch (_currentSize)
			{
				case 1:
					_solos.AddToClassList("match-settings-selection--selected");
					break;
				case 2:
					_duos.AddToClassList("match-settings-selection--selected");
					break;
				case 4:
					_quads.AddToClassList("match-settings-selection--selected");
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