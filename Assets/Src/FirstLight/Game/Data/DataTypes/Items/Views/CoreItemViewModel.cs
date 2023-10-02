using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holder for item generators (e.g cores)
	/// </summary>
	public class CoreItemViewModel : IItemViewModel
	{
		public GameId GameId => _gameId;
		public uint Amount => 1;
		public string DisplayName => GameId.GetLocalization().ToUpper();
		public VisualElement ItemCard => new GameIdIconSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void LegacyRenderSprite(VisualElement icon, Label name, Label amount)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			name.text = DisplayName;
#pragma warning disable CS4014
			UIUtils.SetSprite(GameId, icon);
#pragma warning restore CS4014
		}

		private GameId _gameId;
		public CoreItemViewModel(ItemData item)
		{
			_gameId = item.Id;
		}
	}
}