using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	public class SelectSquadSizePopupView : UIView
	{
		private readonly Action<int> _onSquadSizeSelected;
		private readonly int _currentSize;

		public SelectSquadSizePopupView(Action<int> onSquadSizeSelected, int currentSize)
		{
			_onSquadSizeSelected = onSquadSizeSelected;
			_currentSize = currentSize;
		}

		protected override void Attached()
		{
			var solos = Element.Q<ImageButton>("Solos").Required();
			var duos = Element.Q<ImageButton>("Duos").Required();
			var quads = Element.Q<ImageButton>("Quads").Required();

			switch (_currentSize)
			{
				case 1:
					solos.AddToClassList("match-settings-selection--selected");
					break;
				case 2:
					duos.AddToClassList("match-settings-selection--selected");
					break;
				case 4:
					quads.AddToClassList("match-settings-selection--selected");
					break;
				default:
					throw new InvalidOperationException($"Invalid squad size: {_currentSize}");
			}

			solos.clicked += () => _onSquadSizeSelected.Invoke(1);
			duos.clicked += () => _onSquadSizeSelected.Invoke(2);
			quads.clicked += () => _onSquadSizeSelected.Invoke(4);
		}
	}
}