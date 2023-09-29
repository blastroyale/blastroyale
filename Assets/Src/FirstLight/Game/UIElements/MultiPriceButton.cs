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
		private const string UssBlock = "multi-price-button";
		private const string UssHolder = UssBlock + "__holder";
		private const string UssOffsetTitle = UssBlock + "__offset-title";
		private const string UssPrice = UssBlock + "__price";
		private const string UssPriceInsufficient = UssPrice + "--insufficient";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssSpriteIcon = "sprite-shared__icon-currency-{0}";

		private VisualElement _holder;
		private Label _title;

		private string localizationKey { get; set; }

		public MultiPriceButton()
		{
			AddToClassList(UssButtonStyle);
			AddToClassList(UssBlock);

			Add(_holder = new VisualElement { name = "holder" });
			_holder.AddToClassList(UssHolder);

			Add(_title = new Label("REPAIR") { name = "title" });
		}

		/// <summary>
		/// Sets the price amounts and icons for each cost.
		/// </summary>
		public void SetPrice(Pair<GameId, uint>[] prices,bool isNft, bool[] insufficient, bool overrideDisablePrice = false)
		{
			Remove(_holder);
			Add(_holder = new VisualElement { name = "holder" });
			_holder.AddToClassList(UssHolder);

			for(int i = 0; i < prices.Length; i++)
			{
				Label currentPrice;
				//currentPrice.RemoveModifiers();
				_holder.Add(currentPrice = new Label("123") { name = "price" });
				currentPrice.AddToClassList(UssPrice);
				currentPrice.text = prices[i].Value.ToString();

				VisualElement currentIcon;
				_holder.Add(currentIcon = new VisualElement { name = "icon" });
				currentIcon.AddToClassList(UssIcon);
				currentIcon.AddToClassList(string.Format(UssSpriteIcon, prices[i].Key.ToString().ToLowerInvariant()));
				//currentIcon.RemoveSpriteClasses();

				if (insufficient[i])
				{
					currentPrice.AddToClassList(UssPriceInsufficient);
				}

			}

			_holder.SetDisplay(!isNft);
			_title.EnableInClassList(UssOffsetTitle, isNft);

			if (overrideDisablePrice)
			{
				_holder.SetDisplay(false);
				_title.EnableInClassList(UssOffsetTitle, true);
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