using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
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

		private static IReadOnlyDictionary<GameId, string> _richTextIcons = new Dictionary<GameId, string>()
		{
			{ GameId.COIN, "Coinicon"},
			{ GameId.BlastBuck, "Blastbuckicon"},
			{ GameId.CS, "CraftSpiceicon"},
		};
		
		
		public ItemData Item { get; }
		public GameId GameId => _gameId;
		public uint Amount => _amount;
		public string DisplayName => GameId.GetCurrencyLocalization(_amount).ToUpper();
		public string Description => $"X {_amount}";

		public string ItemTypeDisplayName => GameId.GetCurrencyLocalization(_amount).ToUpperInvariant();

		public VisualElement ItemCard => new CurrencyRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public string GetRichTextIcon()
		{
			if (!_richTextIcons.TryGetValue(Item.Id, out var iconName))
			{
				FLog.Error($"Could not read rich text icon for {Item.Id}");
				iconName = _richTextIcons[GameId.COIN];
			}
			return $"<sprite name=\"{iconName}\">";
		}
		
		public void DrawIcon(VisualElement icon)
		{
			if (MainInstaller.TryResolve<IGameServices>(out var services))
			{
				var config = services.ConfigsProvider.GetConfig<CurrencySpriteConfig>();

				if (config.TryGetConfig(GameId, out var entry))
				{
					var clazz = entry.GetClassForAmount(Amount);
					icon.style.backgroundImage = StyleKeyword.Null;
					icon.RemoveSpriteClasses();
					icon.AddToClassList(clazz);
					return;
				}

				throw new Exception("Unable to set icon for currency " + GameId);
			}
			
		}

		public override string ToString() => GetRichTextIcon();

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