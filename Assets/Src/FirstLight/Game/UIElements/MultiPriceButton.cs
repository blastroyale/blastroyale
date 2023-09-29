using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using BestHTTP.SocketIO3.Parsers;
using FirstLight.FLogger;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button with a title text and a price / icon deisplayed above.
	/// </summary>
	public class MultiPriceButton : ImageButton
	{
		private const string UssButtonStyle = "button-long";
		private const string UssBlock = "price-button";
		private const string UssHolder = UssBlock + "__holder";
		private const string UssOffsetTitle = UssBlock + "__offset-title";
		private const string UssPriceHolder = UssBlock + "__price-holder";
		private const string UssPrice = UssBlock + "__price";
		private const string UssPriceInsufficient = UssPrice + "--insufficient";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssSpriteIcon = "sprite-shared__icon-currency-{0}";

		private Label _price;
		private VisualElement _icon;
		private VisualElement _priceHolder;
		private VisualElement _holder;
		private Label _title;

		private string localizationKey { get; set; }

		public MultiPriceButton()
		{
			AddToClassList(UssButtonStyle);
			AddToClassList(UssBlock);

			var holder = new VisualElement { name = "holder" };
			holder.AddToClassList(UssHolder);

			Add(holder);
			{
				var priceHolder = new VisualElement { name = "price-holder" };
				priceHolder.AddToClassList(UssPriceHolder);

				_holder = holder;
				_priceHolder = priceHolder;

				holder.Add(_title = new Label("REPAIR") { name = "title" });
			}
		}

		/// <summary>
		/// Sets the price amounts and icons for each cost.
		/// </summary>
		public void SetPrice(Pair<GameId, uint>[] prices, bool isNft, bool insufficient = false, bool overrideDisablePrice = false)
		{
			_priceHolder.Remove(_priceHolder);
			var priceHolder = new VisualElement { name = "price-holder" };
			priceHolder.AddToClassList(UssPriceHolder);
			_priceHolder = priceHolder;

			foreach (var cost in prices)
			{
				Label currentPrice;
				VisualElement currentIcon;
				_priceHolder.Add(currentPrice = new Label("123") { name = "price" });
				_priceHolder.Add(currentIcon = new VisualElement { name = "icon" });

				currentPrice.text = cost.Value.ToString();
				currentPrice.RemoveModifiers();
				if (insufficient)
				{
					currentPrice.AddToClassList(UssPriceInsufficient);
				}

				currentIcon.RemoveSpriteClasses();
				currentIcon.AddToClassList(string.Format(UssSpriteIcon, cost.Key.ToString().ToLowerInvariant()));
			}

			_priceHolder.SetDisplay(!isNft);

			if (overrideDisablePrice)
			{
				_priceHolder.SetDisplay(false);
			}
		}

		public new class UxmlFactory : UxmlFactory<MultiPriceButton, UxmlTraits>
		{
		}

		public new class UxmlTraits : ImageButton.UxmlTraits
		{
			UxmlStringAttributeDescription _localizationKeyAttribute = new()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var mpb = (MultiPriceButton) ve;
				mpb.localizationKey = _localizationKeyAttribute.GetValueFromBag(bag, cc);
				mpb._title.text = mpb.localizationKey.LocalizeKey();
			}
		}

	}
}