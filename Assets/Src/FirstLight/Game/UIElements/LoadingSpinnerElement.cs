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
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A small widget responsible for displaying a loading spinner that rotates with the set amount of speed
	/// </summary>
	public class LoadingSpinnerElement : VisualElement, IUIView
	{
		/* Class names are at the top in const fields */
		private const string UssClassName = "loading-spinner";

		/* UXML attributes */
		private float rotationsPerSecond { get; set; }

		/* VisualElements created within this element */

		/* Services, providers etc... */
		private IGameDataProvider _gameDataProvider;
		private IMainMenuServices _mainMenuServices;
		private IGameServices _gameServices;

		/* Other private variables */
		private ValueAnimation<float> _animationTween;
		private VisualElement _rotElement;

		/* The internal structure of the element is created in the constructor. */
		public LoadingSpinnerElement()
		{
			AddToClassList(UssClassName);
		}

		/* IUIView: Called the first time this element is initialized (on first Open) */
		public void Attached(VisualElement visualElement)
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_rotElement = visualElement;
			AnimateRotation();
		}

		/* IUIView: Called by the presenter when the screen is opened */
		public void SubscribeToEvents()
		{
		}

		/* IUIView: Called by the presenter when the screen is closed */
		public void UnsubscribeFromEvents()
		{
			if (_animationTween.isRunning)
			{
				_animationTween.Stop();
			}
		}

		private void AnimateRotation()
		{
			_animationTween = _rotElement.experimental.animation.Start(0f, 1f, (int)(1000 / rotationsPerSecond), (ve, val) =>
			{
				ve.transform.rotation = Quaternion.Euler(0, 0, 360 * val);
			});
			_animationTween.Ease(Easing.Linear);
			_animationTween.KeepAlive();
			_animationTween.OnCompleted(() => { _animationTween.Start(); });
		}

		/* The factory is at the bottom - this allows you to use the element in UXML with it's C# class name */
		public new class UxmlFactory : UxmlFactory<LoadingSpinnerElement, UxmlTraits>
		{
		}

		/* Traits are last, you set up custom UXML attributes here. */
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlFloatAttributeDescription _rotationsPerSecondAttribute = new()
			{
				name = "rotationsPerSecond",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "0", max = "5"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}
			
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var ls = (LoadingSpinnerElement) ve;
				ls.rotationsPerSecond = _rotationsPerSecondAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}