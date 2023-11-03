using System;
using FirstLight.Game.Configs;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	public class UnlockItemViewModel : IItemViewModel
	{
		public ItemData Item { get; }
		public UnlockSystem UnlockSystem { get; }
		public GameId GameId { get; }
		public uint Amount { get; }
		public string Description => null;
		public string DisplayName => UnlockSystem.ToString().ToUpper() + "<br>SCREEN"; // TODO: Move to localizations
		public string ItemTypeDisplayName => "Unlock";

		public VisualElement ItemCard => new UnlockRewardSummaryElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
			switch (UnlockSystem)
			{
				case UnlockSystem.Shop:
					icon.AddToClassList("sprite-home__icon-shop");
					break;
				case UnlockSystem.Collection:
					icon.AddToClassList("sprite-home__icon-heroes");
					break;
				case UnlockSystem.PaidBattlePass:
					icon.AddToClassList("sprite-home__icon-battlepass-crown");
					break;
				case UnlockSystem.Leaderboards:
					icon.AddToClassList("sprite-home__icon-leaderboards");
					break;
				case UnlockSystem.Equipment:
					icon.AddToClassList("sprite-home__icon-equipment");
					break;
				case UnlockSystem.GameModes:
					icon.AddToClassList("sprite-home__icon-marketplace");
					break;
			}
		}

		public UnlockItemViewModel(ItemData item)
		{
			if (item.MetadataType != ItemMetadataType.Unlock)
			{
				throw new Exception($"Building View {GetType().Name} with wrong item type {item}");
			}

			Item = item;
			UnlockSystem = item.GetMetadata<UnlockMetadata>().Unlock;
		}
	}
}