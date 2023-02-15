using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a CollectionScreen card for items like Skins, Banners and Gliders.
	/// </summary>
	public class CollectionCardElement : ImageButton
	{
		private const string ADJECTIVE_LOC_KEY = "UITEquipment/adjective_{0}";

		private const string UssBlock = "equipment-card";
		private const string UssBlockSelected = UssBlock + "--selected";
		private const string UssBlockHighlighted = UssBlock + "--highlighted";

		private const string UssSelected = UssBlock + "__selected-bg";
		private const string UssHighlight = UssBlock + "__highlight";
		private const string UssBackground = UssBlock + "__background";
		private const string UssCardHolder = UssBlock + "__card-holder";
		private const string UssImage = UssBlock + "__image";
		private const string UssImageShadow = UssImage + "--shadow";
		private const string UssName = UssBlock + "__name";
		private const string UssBadgeHolder = UssBlock + "__badge-holder";
		private const string UssBadgeNft = UssBlock + "__badge-nft";
		private const string UssBadgeLoaned = UssBlock + "__badge-loaned";
		private const string UssBadgeEquipped = UssBlock + "__badge-equipped";

		private const string UssNotification = UssBlock + "__notification";
		private const string UssNotificationIcon = "notification-icon";

		public Equipment Equipment { get; private set; }
		public UniqueId UniqueId { get; private set; }

		private readonly VisualElement _nftBadge;
		private readonly VisualElement _equippedBadge;

		private readonly VisualElement _image;
		private readonly VisualElement _imageShadow;
		
		private readonly VisualElement _category;
		private readonly VisualElement _notification;
		
		private readonly Label _name;

		/// <summary>
		/// Triggered when the card is clicked
		/// </summary>
		public new event Action<Equipment, UniqueId> clicked;

		public CollectionCardElement() : this(Equipment.None)
		{
		}

		public CollectionCardElement(Equipment equipment, bool highlighted = false)
		{
			AddToClassList(UssBlock);

			var selectedBg = new VisualElement {name = "selected-bg"};
			Add(selectedBg);
			selectedBg.AddToClassList(UssSelected);

			var highlight = new VisualElement {name = "highlight"};
			Add(highlight);
			highlight.AddToClassList(UssHighlight);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(UssBackground);

			var cardHolder = new VisualElement {name = "holder"};
			Add(cardHolder);
			cardHolder.AddToClassList(UssCardHolder);

			cardHolder.Add(_imageShadow = new VisualElement {name = "equipment-image-shadow"});
			_imageShadow.AddToClassList(UssImage);
			_imageShadow.AddToClassList(UssImageShadow);

			cardHolder.Add(_image = new VisualElement {name = "item-image"});
			_image.AddToClassList(UssImage);

			var badgeHolder = new VisualElement {name = "badge-holder"};
			cardHolder.Add(badgeHolder);
			badgeHolder.AddToClassList(UssBadgeHolder);
			{
				badgeHolder.Add(_nftBadge = new VisualElement {name = "badge-nft"});
				_nftBadge.AddToClassList(UssBadgeNft);

				badgeHolder.Add(
					_equippedBadge = new Label(ScriptLocalization.UITEquipment.equipped) {name = "badge-equipped"});
				_equippedBadge.AddToClassList(UssBadgeEquipped);
			}

			cardHolder.Add(_name = new Label("ROCKET LAUNCHER") {name = "name"});
			_name.AddToClassList(UssName);

			cardHolder.Add(_notification = new VisualElement());
			_notification.AddToClassList(UssNotification);
			_notification.AddToClassList(UssNotificationIcon);

			base.clicked += () => clicked?.Invoke(Equipment, UniqueId);

			if (highlighted)
			{
				AddToClassList(UssBlockHighlighted);
			}

			if (equipment.IsValid())
			{
				SetEquipment(equipment, UniqueId.Invalid);
			}
		}

		public void SetSelected(bool selected)
		{
			if (selected)
			{
				AddToClassList(UssBlockSelected);
			}
			else
			{
				RemoveFromClassList(UssBlockSelected);
			}
		}

		public void SetEquipment(Equipment equipment, UniqueId id, bool loaned = false, bool nft = false,
								 bool equipped = false, bool notification = false, bool loadEditorSprite = false)
		{
			Assert.IsTrue(equipment.IsValid());
			
			_nftBadge.SetDisplay(nft);
			_equippedBadge.SetDisplay(equipped);
			_notification.SetDisplay(notification);

			_name.text = equipment.GameId.GetLocalization();

			Equipment = equipment;

			if (id == UniqueId) return;
			UniqueId = id;

			LoadImage(loadEditorSprite);
		}

		private async void LoadImage(bool loadEditorSprite)
		{
			if (!loadEditorSprite)
			{
				// TODO: This should be handled better.
				var services = MainInstaller.Resolve<IGameServices>();
				_image.style.backgroundImage = null;
				var sprite = await services.AssetResolverService.RequestAsset<GameId, Sprite>(
					Equipment.GameId, instantiate: false);

				if (this.IsAttached())
				{
					_image.style.backgroundImage =
						_imageShadow.style.backgroundImage = new StyleBackground(sprite);
				}
			}
			else
			{
#if UNITY_EDITOR
				_image.style.backgroundImage =
					_imageShadow.style.backgroundImage = new StyleBackground(
						UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
							$"Assets/AddressableResources/Sprites/Equipment/{Equipment.GetEquipmentGroup().ToString()}/{Equipment.GameId.ToString()}.png"));
#endif
			}
		}

		public new class UxmlFactory : UxmlFactory<CollectionCardElement, UxmlTraits>
		{
		}
	}
}