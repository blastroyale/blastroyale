using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class MatchSettingsView : UIView
	{
		private MatchSettingsButtonElement _modeButton;
		private MatchSettingsButtonElement _mapButton;
		private MatchSettingsButtonElement _teamSizeButton;
		private MatchSettingsButtonElement _maxPlayersButton;

		private ScrollView _mutatorsScroller;
		private ImageButton _mutatorsButton;

		protected override void Attached()
		{
			_modeButton = Element.Q<MatchSettingsButtonElement>("ModeButton").Required();
			_teamSizeButton = Element.Q<MatchSettingsButtonElement>("TeamSizeButton").Required();
			_mapButton = Element.Q<MatchSettingsButtonElement>("MapButton").Required();
			_maxPlayersButton = Element.Q<MatchSettingsButtonElement>("MaxPlayersButton").Required();

			_mutatorsScroller = Element.Q<ScrollView>("MutatorsScroller").Required();
			_mutatorsButton = Element.Q<ImageButton>("MutatorsButton").Required();
		}

		public void SetEditable(bool editable)
		{
			Element.SetEnabled(editable);
		}
	}
}