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
		public ItemData Item { get; }
		public GameId GameId => _gameId;
		public uint Amount => _amount;
		public string DisplayName => GameId.GetCurrencyLocalization(_amount).ToUpper();
		public string Description => $"X {_amount}";
		public VisualElement ItemCard => new CurrencyRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
#pragma warning disable CS4014
			UIUtils.SetSprite(GameId, icon);
#pragma warning restore CS4014
		}

		private GameId _gameId;
		private uint _amount;

		public CurrencyItemViewModel(ItemData item)
		{
			Item = item;
			_gameId = item.Id;
			_amount = (uint)item.GetMetadata<CurrencyMetadata>().Amount;
		}
	}
}