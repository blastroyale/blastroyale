using System.Collections.Generic;
using DG.Tweening;
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
	public class CurrencyDisplayElement : VisualElement, IVisualElementLifecycle
	{
		/* Class names are at the top in const fields */
		private const string UssClassName = "currency-display";
		private const string IconUssClassName = "currency-display__icon";
		private const string IconCsUssClassName = "currency-display__icon--cs";
		private const string IconBlstUssClassName = "currency-display__icon--blst";
		private const string LabelUssClassName = "currency-display__label";

		/* UXML attributes */
		private GameId _currency;

		/* VisualElements created within this element */
		private readonly VisualElement _icon;
		private readonly Label _label;

		/* Services, providers etc... */
		private IGameDataProvider _gameDataProvider;
		private IMainMenuServices _mainMenuServices;
		private IGameServices _gameServices;

		/* Other private variables */
		private Tween _animationTween;

		/* The internal structure of the element is created in the constructor. */
		public CurrencyDisplayElement()
		{
			AddToClassList(UssClassName);

			// Currency icon
			_icon = new VisualElement();
			_icon.AddToClassList(IconUssClassName);
			Add(_icon);

			// Currency label
			_label = new Label("1234");
			_label.AddToClassList(LabelUssClassName);
			Add(_label);
		}

		/* IVisualElementLifecycle: Called by the presenter when the screen is opened */
		public void RuntimeInit()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
			_gameServices = MainInstaller.Resolve<IGameServices>();

			_gameDataProvider.CurrencyDataProvider.Currencies.InvokeObserve(_currency, OnCurrencyChanged);
		}

		/* IVisualElementLifecycle: Called by the presenter when the screen is closed */
		public void RuntimeCleanup()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
			_animationTween?.Kill();
		}

		private void OnCurrencyChanged(GameId id, ulong previous, ulong current, ObservableUpdateType type)
		{
			if (_gameDataProvider.RewardDataProvider.IsCollecting)
			{
				AnimateCurrency(previous, current);
			}
			else
			{
				_label.text = current.ToString();
			}
		}

		private void AnimateCurrency(ulong previous, ulong current)
		{
			_animationTween?.Kill();

			_animationTween = DOVirtual.DelayedCall(2f, () =>
			{
				for (int i = 0; i < Mathf.Min(10, current - previous); i++)
				{
					_mainMenuServices.UiVfxService.PlayVfx(_currency,
						i * 0.1f,
						GetRoot().GetPositionOnScreen(GetRoot()) + Random.insideUnitCircle * 100,
						_label.GetPositionOnScreen(GetRoot()),
						() =>
						{
							DOVirtual.Float(previous, current, 0.3f, val => { _label.text = val.ToString("F0"); });
							_gameServices.AudioFxService.PlayClip2D(AudioId.CounterTick1);
						});
				}
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
				restriction = new UxmlEnumeration {values = new[] {GameId.CS.ToString(), GameId.BLST.ToString()}},
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

				cde._currency = _currencyAttribute.GetValueFromBag(bag, cc);
				cde._icon.ClearClassList();
				cde._icon.AddToClassList(IconUssClassName);
				cde._icon.AddToClassList(cde._currency switch
				{
					GameId.BLST => IconBlstUssClassName,
					GameId.CS => IconCsUssClassName,
					_ => ""
				});
			}
		}
	}
}