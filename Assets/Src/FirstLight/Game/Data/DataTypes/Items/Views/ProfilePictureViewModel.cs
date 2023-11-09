using System.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;


namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Profile picture collection items view model
	/// </summary>
	public class ProfilePictureViewModel : IItemViewModel
	{
		public ItemData Item { get; }
		public GameId GameId { get; }
		public uint Amount => 1;
		public string DisplayName { get; }
		public string ItemTypeDisplayName => GameIdGroup.ProfilePicture.GetGameIdGroupLocalization();
		public string Description => null;
		
		public VisualElement ItemCard => new ProfilePictureRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void DrawIcon(VisualElement icon)
		{
			icon.style.backgroundImage = StyleKeyword.Null;
			icon.RemoveSpriteClasses();
			_ = DrawAvatar(icon);
		}

		private async Task DrawAvatar(VisualElement icon)
		{
			var sprite = await MainInstaller.ResolveServices().CollectionService.LoadCollectionItemSprite(Item);
			icon.style.backgroundImage = new StyleBackground(sprite);
			var w = icon.resolvedStyle.width / 2;
			if (w == 0) w = 128;
			icon.style.borderBottomLeftRadius = w;
			icon.style.borderBottomRightRadius = w;
			icon.style.borderTopLeftRadius = w;
			icon.style.borderTopRightRadius = w;
		}
		
		public ProfilePictureViewModel(ItemData item)
		{
			Item = item;
			GameId = item.Id;
			DisplayName = GameId.GetLocalization();
		}
	}
}