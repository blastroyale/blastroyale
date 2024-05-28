using System.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;


namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Profile picture collection items view model
	/// </summary>
	public class ProfilePictureViewModel : IItemViewModel
	{
		private const string USS_AVATAR_ROUNDED_MODIFIER = "avatar--rounded";
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
			// TODO: Move to general USS
			var sprite = await MainInstaller.ResolveServices().CollectionService.LoadCollectionItemSprite(Item);
			icon.style.backgroundImage = new StyleBackground(sprite);
			icon.AddToClassList(USS_AVATAR_ROUNDED_MODIFIER);
		}

		public ProfilePictureViewModel(ItemData item)
		{
			Item = item;
			GameId = item.Id;
			DisplayName = GameId.GetLocalization();
		}
	}
}