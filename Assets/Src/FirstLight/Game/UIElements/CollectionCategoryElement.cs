using System;
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

		public GameIdGroup Category { get; set; }

		private readonly VisualElement _icon;
		private readonly Label _name;

		private readonly VisualElement _locked;
		private readonly VisualElement _notification;

		/// <summary>
		/// Triggered when the card is clicked
		/// </summary>
		public new event Action<GameIdGroup> clicked;

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

			base.clicked += () => clicked?.Invoke(Category);
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

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlEnumAttributeDescription<GameIdGroup> _category = new()
			{
				name = "category",
				defaultValue = GameIdGroup.Glider,
				restriction = new UxmlEnumeration
				{
					values = new[]
					{
						GameIdGroup.Glider.ToString(),
						GameIdGroup.PlayerSkin.ToString(),
						GameIdGroup.DeathMarker.ToString()
					}
				},
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var cce = (CollectionCategoryElement) ve;
				var cat = _category.GetValueFromBag(bag, cc);

				cce.Category = cat;

				cce._icon.RemoveSpriteClasses();
				cce._icon.AddToClassList(cat switch
				{
					GameIdGroup.Glider      => UssSpriteIconGlider,
					GameIdGroup.PlayerSkin  => UssSpriteIconCharacters,
					GameIdGroup.DeathMarker => UssSpriteIconBanner,
					_                       => throw new ArgumentOutOfRangeException()
				});

				cce._name.text = cat switch
				{
					GameIdGroup.Glider      => ScriptLocalization.UITCollectionScreen.gliders,
					GameIdGroup.PlayerSkin  => ScriptLocalization.UITCollectionScreen.banners,
					GameIdGroup.DeathMarker => ScriptLocalization.UITCollectionScreen.characters,
					_                       => throw new ArgumentOutOfRangeException()
				};
			}
		}
	}
}