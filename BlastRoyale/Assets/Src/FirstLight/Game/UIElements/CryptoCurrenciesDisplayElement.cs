using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Widget that holds a main currency (Noob) and a panel with all other currencies that the player owns and it's supported by our game
	/// </summary>
	public class CryptoCurrenciesDisplayElement : VisualElement
	{
		/* Class names are at the top in const fields */
		//Default Currency
		private const string USS_BLOCK = "currency-display";
		private const string USS_BLOCK_CRYPTO_MODIFIER = USS_BLOCK + "--crypto";
		private const string USS_ICON = USS_BLOCK + "__icon";
		private const string USS_ICON_OUTLINE = USS_BLOCK + "__icon-outline";
		private const string USS_CURRENCY_LABEL = USS_BLOCK + "__label";
		private const string USS_BLOCK_PLUSSIGN = USS_BLOCK + USS_PLUSSIGN_MODIFIER;
		private const string USS_MULTIPLE_CURRENCY_LABEL = USS_CURRENCY_LABEL + USS_PLUSSIGN_MODIFIER;

		//Crypto Currency

		private const string USS_CRYPTO_PARENT = "crypto-currencies-parent";
		private const string USS_CRYPTO_PARENT_PLUS = "crypto-currencies-parent" + USS_PLUSSIGN_MODIFIER;
		private const string USS_CRYPTO_CURRENCY_BLOCK = "crypto-currencies-display";
		private const string USS_CRYPTO_CURRENCY_BLOCK_ARROW = USS_CRYPTO_CURRENCY_BLOCK + "__arrow";
		private const string USS_INNER_ELEMENT_CRYPTO_CURRENCY_BLOCK = USS_BLOCK + "__inner-element-crypto-currency";
		private const string USS_CRYPTO_CURRENCY_COLUMN = "crypto-currencies-column";
		private const string USS_CRYPTO_CURRENCY_SCROLLVIEW_CONTAINER = USS_CRYPTO_CURRENCY_BLOCK + "__scrollview-content-container";

		/*Modifier Constant*/
		private const string USS_PLUSSIGN_MODIFIER = "__plussign";

		/* UXML attributes */
		public GameId MainCurrency { get; private set; }

		/* VisualElements created within this element */
		private readonly VisualElement _mainCryptoCurrencyIcon;
		private readonly VisualElement _mainCryptoCurrencyIconOutline;
		private readonly Label _mainCryptoCurrencyAmount;
		private readonly VisualElement _partnerCryptoCurrenciesContainer;
		private readonly VisualElement _partnerCryptoCurrenciesArrow;

		private Dictionary<GameId, ulong> _cryptoCurrenciesDict;
		private Action OnClickedAction;
		private VisualElement _buttonView;
		private CurrencyDisplayElement.CurrencyAnimationHandler _animationHandler;

		private bool IsOnlyMainCurrency => _cryptoCurrenciesDict.Count == 1 && _cryptoCurrenciesDict.First().Key == MainCurrency;

		/* The internal structure of the element is created in the constructor. */
		public CryptoCurrenciesDisplayElement()
		{
			_animationHandler = new CurrencyDisplayElement.CurrencyAnimationHandler();
			AddToClassList(USS_CRYPTO_PARENT);
			{
				_buttonView = new VisualElement();
				_buttonView.AddToClassList(USS_BLOCK);
				_buttonView.AddToClassList(USS_BLOCK_CRYPTO_MODIFIER);
				_buttonView.RegisterCallback<ClickEvent>(OnClicked);

				// Icon outline
				_mainCryptoCurrencyIconOutline = new VisualElement() {name = "MainCurrencyIconContainer"};
				_mainCryptoCurrencyIconOutline.AddToClassList(USS_ICON_OUTLINE);
				_buttonView.Add(_mainCryptoCurrencyIconOutline);

				// Currency icon
				_mainCryptoCurrencyIcon = new VisualElement() {name = "MainCurrencyIcon"};
				_mainCryptoCurrencyIcon.AddToClassList(USS_ICON);
				_mainCryptoCurrencyIconOutline.Add(_mainCryptoCurrencyIcon);

				// Currency label
				_mainCryptoCurrencyAmount = new LabelOutlined("1234") {name = "MainCurrencyAmount"};
				_mainCryptoCurrencyAmount.AddToClassList(USS_CURRENCY_LABEL);
				_buttonView.Add(_mainCryptoCurrencyAmount);
				Add(_buttonView);
			}

			// Multiple Currencies Container Arrow
			_partnerCryptoCurrenciesArrow = new VisualElement() {name = "PartnerCryptoCurrenciesArrow"};
			_partnerCryptoCurrenciesArrow.AddToClassList(USS_CRYPTO_CURRENCY_BLOCK_ARROW);

			// Multiple Currencies Container
			_partnerCryptoCurrenciesContainer = new VisualElement() {name = "PartnerCryptoCurrenciesContainer"};
			_partnerCryptoCurrenciesContainer.AddToClassList(USS_CRYPTO_CURRENCY_BLOCK);
			_partnerCryptoCurrenciesContainer.Add(_partnerCryptoCurrenciesArrow);
			Add(_partnerCryptoCurrenciesContainer);

			_partnerCryptoCurrenciesContainer.SetVisibility(false);
		}

		public void SetAnimationOrigin(VisualElement origin)
		{
			_animationHandler.Origin = origin;
		}

		public void AnimateCurrencyEffect(GameId id, ulong previous, ulong amount, CancellationToken cc)
		{
			_animationHandler.CancellationToken = cc;
			_animationHandler.Target = IsOnlyMainCurrency ? _mainCryptoCurrencyAmount : this;
			_animationHandler.Root = this.GetRoot();
			_animationHandler.GameId = id;
			_animationHandler.AnimateCurrency(previous, amount).Forget();
		}

		public void SetMainCurrency(GameId gameId)
		{
			MainCurrency = gameId;
			UpdateMainCurrencyView();
		}

		private void OnClicked(ClickEvent evt)
		{
			if (_cryptoCurrenciesDict.Count == 1 && _cryptoCurrenciesDict.First().Key == MainCurrency)
			{
				this.OpenTooltip(panel.visualTree, MainCurrency.GetDescriptionLocalization());
				return;
			}

			OpenCryptoTokenContainer();
		}

		private void OpenCryptoTokenContainer()
		{
			_partnerCryptoCurrenciesContainer.SetVisibility(!_partnerCryptoCurrenciesContainer.visible);
		}

		private void UpdateMainCurrencyView()
		{
			var mainCurrencyViewModel = (CurrencyItemViewModel) ItemFactory.Currency(MainCurrency, 0).GetViewModel();

			_mainCryptoCurrencyIcon.ClearClassList();
			_mainCryptoCurrencyIcon.AddToClassList(USS_ICON);
			mainCurrencyViewModel.DrawIcon(_mainCryptoCurrencyIcon);

			_mainCryptoCurrencyIconOutline.ClearClassList();
			_mainCryptoCurrencyIconOutline.AddToClassList(USS_ICON_OUTLINE);
			mainCurrencyViewModel.DrawIcon(_mainCryptoCurrencyIconOutline);
		}

		public void SetData(Dictionary<GameId, ulong> playerCryptoCurrenciesDict)
		{
			_cryptoCurrenciesDict = playerCryptoCurrenciesDict;
			var shouldHideElement = playerCryptoCurrenciesDict.Count == 0;

			this.SetDisplay(!shouldHideElement);

			if (!shouldHideElement)
			{
				SetupCurrenciesDisplay(playerCryptoCurrenciesDict);
			}
		}

		private void SetupCurrenciesDisplay(Dictionary<GameId, ulong> cryptoCurrenciesDict)
		{
			// Only the main currency is available, maintain current behavior (shows NoobAmount)
			if (cryptoCurrenciesDict.Count == 1 && cryptoCurrenciesDict.First().Key == MainCurrency)
			{
				_mainCryptoCurrencyAmount.text = cryptoCurrenciesDict[MainCurrency].ToString();
				return;
			}

			//Start Setting Up Tooltip with Multiples Crypto Tokens
			AddToClassList(USS_CRYPTO_PARENT_PLUS);
			_buttonView.AddToClassList(USS_BLOCK_PLUSSIGN);
			_mainCryptoCurrencyAmount.AddToClassList(USS_MULTIPLE_CURRENCY_LABEL);
			_mainCryptoCurrencyAmount.text = "+";

			if (cryptoCurrenciesDict.Count <= 8)
			{
				SetupCryptoTokenColumns(cryptoCurrenciesDict);
				return;
			}

			SetupCryptoTokenScrollView(cryptoCurrenciesDict);
		}

		private void SetupCryptoTokenScrollView(Dictionary<GameId, ulong> cryptoCurrenciesDict)
		{
			_partnerCryptoCurrenciesContainer.Clear();
			_partnerCryptoCurrenciesContainer.Add(_partnerCryptoCurrenciesArrow);
			var cryptoTokenScrollView = new ScrollView();
			cryptoTokenScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
			cryptoTokenScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

			cryptoTokenScrollView.contentContainer.AddToClassList(USS_CRYPTO_CURRENCY_SCROLLVIEW_CONTAINER);

			foreach (var cryptoCurrency in cryptoCurrenciesDict)
			{
				var currencyDisplayElement = new CurrencyDisplayElement();
				currencyDisplayElement.AddToClassList(USS_INNER_ELEMENT_CRYPTO_CURRENCY_BLOCK);
				currencyDisplayElement.SetCurrency(cryptoCurrency.Key, cryptoCurrency.Value);
				cryptoTokenScrollView.Add(currencyDisplayElement);
			}

			_partnerCryptoCurrenciesContainer.Add(cryptoTokenScrollView);
		}

		private void SetupCryptoTokenColumns(Dictionary<GameId, ulong> cryptoCurrenciesDict)
		{
			_partnerCryptoCurrenciesContainer.Clear();
			_partnerCryptoCurrenciesContainer.Add(_partnerCryptoCurrenciesArrow);
			VisualElement currentCryptoTokenColumn = null;
			var cryptoTokensAdded = 0;

			foreach (var cryptoCurrency in cryptoCurrenciesDict)
			{
				if (cryptoTokensAdded % 4 == 0)
				{
					if (currentCryptoTokenColumn != null)
					{
						_partnerCryptoCurrenciesContainer.Add(currentCryptoTokenColumn);
					}

					currentCryptoTokenColumn = new VisualElement();
					currentCryptoTokenColumn.AddToClassList(USS_CRYPTO_CURRENCY_COLUMN);
				}

				var currencyDisplayElement = new CurrencyDisplayElement();
				currencyDisplayElement.AddToClassList(USS_INNER_ELEMENT_CRYPTO_CURRENCY_BLOCK);
				currencyDisplayElement.SetCurrency(cryptoCurrency.Key, cryptoCurrency.Value);

				currentCryptoTokenColumn.Add(currencyDisplayElement);

				cryptoTokensAdded++;
			}

			// Make sure to add the last group if it hasn't been added yet
			if (currentCryptoTokenColumn != null)
			{
				_partnerCryptoCurrenciesContainer.Add(currentCryptoTokenColumn);
			}
		}

		/* The factory is at the bottom - this allows you to use the element in UXML with it's C# class name */
		public new class UxmlFactory : UxmlFactory<CryptoCurrenciesDisplayElement, UxmlTraits>
		{
		}

		/* Traits are last, you set up custom UXML attributes here. */
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			/* This is a custom attribute that can be set from UXML/UI Builder.
			   It's a GameID enum for MainCurrency, which determines the Token Icon shown on the left side of the element (e.g., Noob).
			   It also defines the Token used to apply visual rules. */
			private readonly UxmlEnumAttributeDescription<GameId> _mainCurrencyAttribute = new ()
			{
				name = "main-currency",
				defaultValue = GameId.NOOB,
				restriction = new UxmlEnumeration {values = GameIdGroup.CryptoCurrency.GetIds().Select(id => id.ToString()).ToArray()},
				use = UxmlAttributeDescription.Use.Required
			};

			/* Tells UIT that this element cannot have children (see base implementation for more info) */
			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			/* Custom attributes are initialized / set to the VisualElement here. */
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var cde = (CryptoCurrenciesDisplayElement) ve;

				cde.SetMainCurrency(_mainCurrencyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}