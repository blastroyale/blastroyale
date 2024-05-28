using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a glint over an element. Must be used with a vector mask for shaped glints.
	/// </summary>
	public class GlintElement : VisualElement
	{
		private const string UssBlock = "glint-element";
		private const string UssGlint = UssBlock + "__glint";
		private const string UssGlintSwiped = UssGlint + "--swiped";

		public bool AutoStart { get; private set; }

		private readonly GradientElement _glint;

		public GlintElement()
		{
			AddToClassList(UssBlock);

			Add(_glint = new GradientElement {name = "glint"});
			_glint.AddToClassList(UssGlint);

			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
		}

		/// <summary>
		/// Triggers a swipe glint across the element.
		/// </summary>
		public void StartGlint(bool loop)
		{
			_glint.RemoveFromClassList(UssGlintSwiped);

			if (loop)
			{
				_glint.RegisterCallback<TransitionEndEvent>(OnGlintTransitionEnd);
			}

			_glint.schedule.Execute(() => _glint.AddToClassList(UssGlintSwiped)).StartingIn(100);
		}

		/// <summary>
		/// Stops the glint animation (will animate the current glint completely before stopping).
		/// </summary>
		public void StopGlint()
		{
			_glint.UnregisterCallback<TransitionEndEvent>(OnGlintTransitionEnd);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			if (AutoStart)
			{
				StartGlint(true);
			}
		}

		private void OnGlintTransitionEnd(TransitionEndEvent evt)
		{
			_glint.RemoveFromClassList(UssGlintSwiped);
			_glint.schedule.Execute(() => _glint.AddToClassList(UssGlintSwiped)).StartingIn(10);
		}

		public new class UxmlFactory : UxmlFactory<GlintElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlBoolAttributeDescription _autoStartAttribute = new()
			{
				name = "auto-start",
				defaultValue = true
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var ge = (GlintElement) ve;
				ge.AutoStart = _autoStartAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}