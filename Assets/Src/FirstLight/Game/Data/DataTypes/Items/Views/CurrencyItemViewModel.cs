using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holder for currency rewards, ex Coins
	/// </summary>
	public class CurrencyItemViewModel : IItemViewModel
	{
		public GameId GameId => _gameId;
		public uint Amount => _amount;
		public string DisplayName => GameId.GetCurrencyLocalization(_amount);
		public VisualElement ItemCard => new CurrencyRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void LegacyRenderSprite(VisualElement icon, Label name, Label amount)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			if(amount != null) amount.text = $"X {Amount}";
			name.text = DisplayName;
#pragma warning disable CS4014
			UIUtils.SetSprite(GameId, icon);
#pragma warning restore CS4014
		}

		private GameId _gameId;
		private uint _amount;

		public CurrencyItemViewModel(ItemData item)
		{
			_gameId = item.Id;
			_amount = (uint)item.GetMetadata<CurrencyMetadata>().Amount;
		}
	}
}