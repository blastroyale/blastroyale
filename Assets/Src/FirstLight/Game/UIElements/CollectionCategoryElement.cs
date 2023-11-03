using System;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a category of items in the collection screen, e.g. Characters, Banners, Gliders, etc.
	/// </summary>
	public class CollectionCategoryElement : ImageButton
	{
		private const string UssBlock = "collection-category";

		private const string UssBlockSelected = UssBlock + "--selected";

		private const string UssHolder = UssBlock + "__holder";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssName = UssBlock + "__name";
		private const string UssNotification = UssBlock + "__notification";

		private const string UssNotificationIcon = "notification-icon";
		private const string UssSpriteIconBanner = "sprite-home__icon-banner";
		private const string UssSpriteIconGlider = "sprite-home__icon-jetpack";
		private const string UssSpriteIconCharacters = "sprite-home__icon-characters";
		private const string UssSpriteIconHammer = "sprite-home__icon-melee-skin";
		private const string UssSpriteIconProfilePicture = "sprite-home__icon-profilepicture";
		
		public CollectionCategory Category { get; set; }

		private readonly VisualElement _icon;
		private readonly Label _name;

		private readonly VisualElement _locked;
		private readonly VisualElement _notification;

		/// <summary>
		/// Triggered when the card is clicked
		/// </summary>
		public new event Action<CollectionCategory> clicked;

		public CollectionCategoryElement()
		{
			AddToClassList(UssBlock);

			var cardHolder = new VisualElement {name = "holder"};
			{
				Add(cardHolder);
				cardHolder.AddToClassList(UssHolder);
				cardHolder.Add(_icon = new VisualElement {name = "icon"});
				_icon.AddToClassList(UssIcon);
				_icon.AddToClassList(UssSpriteIconCharacters);

				cardHolder.Add(_name = new Label("CHARACTERS") {name = "name"});
				_name.AddToClassList(UssName);

				cardHolder.Add(_notification = new VisualElement {name = "notification"});
				_notification.AddToClassList(UssNotification);
				_notification.AddToClassList(UssNotificationIcon);
			}

			SetNotification(false);

			base.clicked += () => clicked?.Invoke(Category);
		}

		public void SetupCategoryButton(CollectionCategory cat)
		{
			Category = cat;
			_icon.RemoveSpriteClasses();
			_icon.AddToClassList(cat.Id switch
			{
				GameIdGroup.Glider      => UssSpriteIconGlider,
				GameIdGroup.PlayerSkin  => UssSpriteIconCharacters,
				GameIdGroup.DeathMarker => UssSpriteIconBanner,
				GameIdGroup.MeleeSkin   => UssSpriteIconHammer,
				GameIdGroup.ProfilePicture   => UssSpriteIconProfilePicture,
				_                       => "",
			});
			_name.text = cat.GetCollectionCategoryDisplayName();
		}

		/// <summary>
		/// TODO
		/// </summary>
		public void SetNotification(bool enabled)
		{
			_notification.SetDisplay(enabled);
		}

		/// <summary>
		/// TODO
		/// </summary>
		public void SetSelected(bool selected)
		{
			EnableInClassList(UssBlockSelected, selected);
		}

		public new class UxmlFactory : UxmlFactory<CollectionCategoryElement, UxmlTraits>
		{
		}
	}
}