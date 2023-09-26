using System.Collections.Generic;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A small widget displaying the selected currency with it's icon. Also handles flying VFX for it.
	/// </summary>
	public class CurrencyDisplayElement : VisualElement
	{
		/* Class names are at the top in const fields */
		private const string UssBlock = "currency-display";
		private const string UssIcon = UssBlock + "__icon";
		private const string UssIconOutline = UssBlock + "__icon-outline";
		private const string LabelUssClassName = UssBlock + "__label";

		private const string UssSpriteCurrency = "sprite-shared__icon-currency-{0}";

		/* UXML attributes */
		private GameId currency { get; set; }

		/* VisualElements created within this element */
		private readonly VisualElement _icon;
		private readonly VisualElement _iconOutline;
		private readonly Label _label;
		/* Services, providers etc... */
		private IGameDataProvider _gameDataProvider;
		private IMainMenuServices _mainMenuServices;
		private IGameServices _gameServices;

		/* Other private variables */
		private Tween _animationTween;
		private VisualElement _originElement;
		private bool _playingAnimation;

		/* The internal structure of the element is created in the constructor. */
		public CurrencyDisplayElement()
		{
			AddToClassList(UssBlock);

			// Icon outline
			_iconOutline = new VisualElement();
			_iconOutline.AddToClassList(UssIconOutline);
			Add(_iconOutline);

			// Currency icon
			_icon = new VisualElement();
			_icon.AddToClassList(UssIcon);
			_iconOutline.Add(_icon);

			// Currency label
			_label = new Label("1234");
			_label.AddToClassList(LabelUssClassName);
			Add(_label);

			RegisterCallback<ClickEvent>(OnClicked);
		}

		private void OnClicked(ClickEvent evt)
		{
			this.OpenTooltip(panel.visualTree, currency.GetDescriptionLocalization());
		}

		public void Init(IGameDataProvider gameDataProvider, IMainMenuServices mainMenuServices, IGameServices gameServices)
		{
			_gameDataProvider = gameDataProvider;
			_mainMenuServices = mainMenuServices;
			_gameServices = gameServices;
		}

		public void SubscribeToEvents()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.Observe(currency, OnCurrencyChanged);
			_label.text = _gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(currency).ToString();
		}

		public void UnsubscribeFromEvents()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
			_animationTween?.Kill();
		}

		/// <summary>
		/// Sets the origin of the currency flying animation starting at another visual element
		/// </summary>
		public void SetAnimationOrigin(VisualElement originElement)
		{
			_originElement = originElement;
		}

		private void OnCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType type)
		{
			if (!_playingAnimation && current > previous) AnimateCurrency(previous, current);
			else _label.text = current.ToString();
		}

		private void AnimateCurrency(ulong previous, ulong current)
		{
			_playingAnimation = true;
			_animationTween?.Kill();

			_animationTween = DOVirtual.DelayedCall(0.1f, () =>
			{
				for (int i = 0; i < Mathf.Min(10, current - previous); i++)
				{
					var originPosition = _originElement != null
						? _originElement.GetPositionOnScreen(GetRoot())
						: GetRoot().GetPositionOnScreen(GetRoot()) + Random.insideUnitCircle * 100;

					_mainMenuServices.UiVfxService.PlayVfx(currency,
						i * 0.1f,
						originPosition,
						_label.GetPositionOnScreen(GetRoot()),
						() =>
						{
							DOVirtual.Float(previous, current, 0.3f, val => { _label.text = val.ToString("F0"); });
							_gameServices.AudioFxService.PlayClip2D(AudioId.CounterTick1);
						});
				}
				_playingAnimation = false;
			});
		}

		private VisualElement GetRoot()
		{
			var p = parent;

			while (p.parent != null)
			{
				p = p.parent;
			}

			return p;
		}

		/* The factory is at the bottom - this allows you to use the element in UXML with it's C# class name */
		public new class UxmlFactory : UxmlFactory<CurrencyDisplayElement, UxmlTraits>
		{
		}

		/* Traits are last, you set up custom UXML attributes here. */
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			/* This is a custom attribute (that can be set from UXML / UI Builder. In this example it's a GameID enum for Currency */
			private readonly UxmlEnumAttributeDescription<GameId> _currencyAttribute = new()
			{
				name = "currency",
				defaultValue = GameId.CS,
				restriction = new UxmlEnumeration
					{values = new[] {GameId.CS.ToString(), GameId.BLST.ToString(), GameId.COIN.ToString()}},
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

				var cde = (CurrencyDisplayElement) ve;

				cde.currency = _currencyAttribute.GetValueFromBag(bag, cc);
				cde._icon.ClearClassList();
				cde._icon.AddToClassList(UssIcon);
				cde._icon.AddToClassList(string.Format(UssSpriteCurrency, cde.currency.ToString().ToLowerInvariant()));

				cde._iconOutline.ClearClassList();
				cde._iconOutline.AddToClassList(UssIconOutline);
				cde._iconOutline.AddToClassList(string.Format(UssSpriteCurrency,
					cde.currency.ToString().ToLowerInvariant()));
			}
		}
	}
}