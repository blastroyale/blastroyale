using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holder for currency rewards, ex Coins
	/// </summary>
	public class CurrencyItemViewModel : IItemViewModel
	{
		private const string USS_SPRITE_REWARD = "sprite-home__reward-{0}";

		public ItemData Item { get; }
		public GameId GameId => _gameId;
		public uint Amount => _amount;
		public string DisplayName => GameId.GetCurrencyLocalization(_amount).ToUpper();
		public string Description => $"X {_amount}";

		public string ItemTypeDisplayName => GameIdGroup.Currency.GetGameIdGroupLocalization();

		public VisualElement ItemCard => new CurrencyRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.RemoveSpriteClasses();
			icon.AddToClassList(string.Format(USS_SPRITE_REWARD, GameId.ToString().ToLowerInvariant()));
		}

		private GameId _gameId;
		private uint _amount;

		public CurrencyItemViewModel(ItemData item)
		{
			Item = item;
			_gameId = item.Id;
			_amount = (uint) item.GetMetadata<CurrencyMetadata>().Amount;
		}
	}
}
