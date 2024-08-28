using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
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

		/* UXML attributes */
		private GameId Currency { get; set; }
		private bool _hideIfPlayerDoesntHaveIt = false;

		/* VisualElements created within this element */
		private readonly VisualElement _icon;
		private readonly VisualElement _iconOutline;
		private readonly Label _label;

		/* Services, providers etc... */
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private CurrencyItemViewModel _currencyView;

		/* Other private variables */
		private CancellationToken _cancellationToken;
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
			_label = new LabelOutlined("1234") {name = "CurrencyAmount"};
			_label.AddToClassList(LabelUssClassName);
			Add(_label);

			RegisterCallback<ClickEvent>(OnClicked);
		}

		public void SetCurrency(GameId gameId)
		{
			Currency = gameId;
			UpdateCurrencyView();
		}

		private void UpdateCurrencyView()
		{
			_currencyView = (CurrencyItemViewModel) ItemFactory.Currency(Currency, 0).GetViewModel();

			_icon.ClearClassList();
			_icon.AddToClassList(UssIcon);
			_currencyView.DrawIcon(_icon);

			_iconOutline.ClearClassList();
			_iconOutline.AddToClassList(UssIconOutline);
			_currencyView.DrawIcon(_iconOutline);
		}

		private void OnClicked(ClickEvent evt)
		{
			this.OpenTooltip(panel.visualTree, Currency.GetDescriptionLocalization());
		}

		public void Init(IGameDataProvider gameDataProvider, IGameServices gameServices)
		{
			_gameDataProvider = gameDataProvider;
			_services = gameServices;
		}

		public void SubscribeToEvents()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.Observe(Currency, OnCurrencyChanged);
			var amount = _gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(Currency);
			this.SetDisplay(amount > 0 || !_hideIfPlayerDoesntHaveIt);
			_label.text = amount.ToString();
		}

		public void UnsubscribeFromEvents()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
			_animationTween?.Kill();
		}

		/// <summary>
		/// Sets the origin of the currency flying animation starting at another visual element
		/// </summary>
		public void SetData(VisualElement animationOrigin, bool hideIfPlayerDoesntHaveIt = false, CancellationToken cancellationToken = default)
		{
			_originElement = animationOrigin;
			_hideIfPlayerDoesntHaveIt = hideIfPlayerDoesntHaveIt;
			_cancellationToken = cancellationToken;
		}

		private void OnCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType type)
		{
			this.SetDisplay(current > 0 || !_hideIfPlayerDoesntHaveIt);
			if (!_playingAnimation && current > previous)
			{
				AnimateCurrency(previous, current).Forget();
			}
			else
			{
				_label.text = current.ToString();
			}
		}

		private async UniTaskVoid AnimateCurrency(ulong previous, ulong current)
		{
			_playingAnimation = true;
			_animationTween?.Kill();

			await UniTask.WaitUntil(() => _label.worldBound.Overlaps(GetRoot().worldBound), cancellationToken: _cancellationToken);
			// Wait for currency view animation to finish
			await UniTask.Delay(500, cancellationToken: _cancellationToken);
			var labelPosition = _label.GetPositionOnScreen(GetRoot());
			for (int i = 0; i < Mathf.Min(10, current - previous); i++)
			{
				var originPosition = _originElement != null
					? _originElement.GetPositionOnScreen(GetRoot())
					: GetRoot().GetPositionOnScreen(GetRoot()) + Random.insideUnitCircle * 100;

				_services.UIVFXService.PlayVfx(Currency,
					i * 0.1f,
					originPosition,
					labelPosition,
					() =>
					{
						DOVirtual.Float(previous, current, 0.3f, val => { _label.text = val.ToString("F0"); });
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);
					});
			}

			_playingAnimation = false;
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
			private readonly UxmlEnumAttributeDescription<GameId> _currencyAttribute = new ()
			{
				name = "currency",
				defaultValue = GameId.CS,
				restriction = new UxmlEnumeration
					{values = GameIdGroup.Currency.GetIds().Select(id => id.ToString()).ToArray()},
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

				cde.SetCurrency(_currencyAttribute.GetValueFromBag(bag, cc));
			}
		}
	}
}