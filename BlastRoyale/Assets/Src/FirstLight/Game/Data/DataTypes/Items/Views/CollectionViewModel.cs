using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Collection items view model
	/// </summary>
	public class CollectionViewModel : IItemViewModel
	{
		public ItemData Item { get; }
		public GameId GameId { get; }
		public uint Amount { get; }
		public string DisplayName { get; }
		
		public string ItemTypeDisplayName => Item.GetCollectionCategory().GetCollectionCategoryDisplayName();

		public VisualElement ItemCard => new CollectionRewardsSummaryElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.style.backgroundImage = StyleKeyword.Null;
			icon.RemoveSpriteClasses();
			DrawDynamicIconAsync(icon).Forget();
		}

		private async UniTask DrawDynamicIconAsync(VisualElement icon)
		{
			try
			{
				var sprite = await MainInstaller.ResolveServices().CollectionService.LoadCollectionItemSprite(Item);
				icon.style.backgroundImage = new StyleBackground(sprite);
			}
			catch (Exception e)
			{
				FLog.Warn("Error rendering texture, using sprite fallback", e);
				icon.AddToClassList(GameId.GetUSSSpriteClass());
			}
		}
		
		public string Description => null;

		public CollectionViewModel(ItemData item)
		{
			Item = item;
			GameId = item.Id;
			Amount = 1;
			DisplayName = GameId.GetLocalization().ToUpperInvariant();
		}
	}
}