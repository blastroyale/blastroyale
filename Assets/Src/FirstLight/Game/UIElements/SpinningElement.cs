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
	public class SpinningElement : VisualElement
	{
		private float rotationsPerSecond { get; set; }
		
		private ValueAnimation<float> _animationTween;

		private void AnimateRotation()
		{
			if (_animationTween != null && _animationTween.isRunning)
			{
				_animationTween.Recycle();
			}
			
			_animationTween = experimental.animation.Start(0f, 1f, (int)(1000 / rotationsPerSecond), (ve, val) =>
			{
				ve.transform.rotation = Quaternion.Euler(0, 0, 360 * val);
			});
			_animationTween.Ease(Easing.Linear);
			_animationTween.KeepAlive();
			_animationTween.OnCompleted(() => { _animationTween.Start(); });
		}
		
		public new class UxmlFactory : UxmlFactory<SpinningElement, UxmlTraits>
		{
		}
		
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlFloatAttributeDescription _rotationsPerSecondAttribute = new()
			{
				name = "rotationsPerSecond",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "0", max = "5"},
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var se = (SpinningElement) ve;
				se.rotationsPerSecond = _rotationsPerSecondAttribute.GetValueFromBag(bag, cc);
				se.AnimateRotation();
			}
		}
	}
}