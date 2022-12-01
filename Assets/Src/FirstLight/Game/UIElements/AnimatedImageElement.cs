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
	public class AnimatedImageElement : VisualElement
	{
		private float rotationsPerSecond { get; set; }
		private float randPosMinX { get; set; }
		private float randPosMinY { get; set; }
		private float randPosMaxX { get; set; }
		private float randPosMaxY { get; set; }
		private int randPosDurationMs { get; set; }
		private float randPosLerpFactor { get; set; }
		private float sineWavePosMinX { get; set; }
		private float sineWavePosMinY { get; set; }
		private float sineWavePosMaxX { get; set; }
		private float sineWavePosMaxY { get; set; }
		private float sineWavePosXOffset { get; set; }
		private float sineWavePosYOffset { get; set; }
		private float sineWavePosXFrequency { get; set; }
		private float sineWavePosYFrequency { get; set; }
		
		private ValueAnimation<float> _rotationTween;
		private ValueAnimation<float> _randPosTween;
		private ValueAnimation<float> _sinewavePosTween;
		private void AnimateRotation()
		{
			if (_rotationTween != null && _rotationTween.isRunning)
			{
				_rotationTween.Recycle();
			}
			
			_rotationTween = experimental.animation.Start(0f, 1f, (int)(1000 / rotationsPerSecond), (ve, percent) =>
			{
				ve.transform.rotation = Quaternion.Euler(0, 0, 360 * percent);
			});
			
			_rotationTween.Ease(Easing.Linear);
			_rotationTween.KeepAlive();
			_rotationTween.OnCompleted(() => { _rotationTween.Start(); });
		}
		
		private void AnimateRandomPosition()
		{
			if (_randPosTween != null && _randPosTween.isRunning)
			{
				_randPosTween.Recycle();
			}

			var randomPos = new Vector2(Random.Range(randPosMinX, randPosMaxX), Random.Range(randPosMinY, randPosMaxY));
			
			_randPosTween = experimental.animation.Start(0f, 1f, randPosDurationMs, (ve, percent) =>
			{
				Debug.LogError(percent);
				var lerpNewX = Mathf.Lerp(ve.transform.position.x, randomPos.x, percent/randPosLerpFactor);
				var lerpNewY = Mathf.Lerp(ve.transform.position.y, randomPos.y, percent/randPosLerpFactor);
				ve.transform.position = new Vector3(lerpNewX, lerpNewY, 0);
			});
			
			_randPosTween.Ease(Easing.Linear);
			_randPosTween.KeepAlive();
			_randPosTween.OnCompleted(() =>
			{
				randomPos = new Vector2(Random.Range(randPosMinX, randPosMaxX), Random.Range(randPosMinY, randPosMaxY));
				_randPosTween.Start();
			});
		}
		
		private void AnimateSineWavePosition()
		{
			if (_sinewavePosTween != null && _sinewavePosTween.isRunning)
			{
				_sinewavePosTween.Recycle();
			}
			
			_sinewavePosTween = experimental.animation.Start(0f, 1f, 9999999, (ve, percent) =>
			{
				var sinX = Mathf.Sin((Time.realtimeSinceStartup*sineWavePosXFrequency) + sineWavePosXOffset);
				var sinY = Mathf.Sin((Time.realtimeSinceStartup*sineWavePosYFrequency) + sineWavePosYOffset);
				var normX = (sinX - -1)/(1 - -1);
				var normY = (sinY - -1)/(1 - -1);
				var lerpNewX = Mathf.Lerp(sineWavePosMinX, sineWavePosMaxX, normX);
				var lerpNewY = Mathf.Lerp(sineWavePosMinY, sineWavePosMaxY, normY);
				
				ve.transform.position = new Vector3(lerpNewX, lerpNewY, 0);
			});
			
			_sinewavePosTween.Ease(Easing.Linear);
			_sinewavePosTween.KeepAlive();
			_sinewavePosTween.OnCompleted(() =>
			{
				_sinewavePosTween.Start();
			});
		}
		
		public new class UxmlFactory : UxmlFactory<AnimatedImageElement, UxmlTraits>
		{
		}
		
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlFloatAttributeDescription _rotationsPerSecondAttribute = new()
			{
				name = "rotationsPerSecond",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _randPosMinXAttribute = new()
			{
				name = "randPosMinX",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _randPosMinYAttribute = new()
			{
				name = "randPosMinY",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _randPosMaxXAttribute = new()
			{
				name = "randPosMaxX",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _randPosMaxYAttribute = new()
			{
				name = "randPosMaxY",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlIntAttributeDescription _randPosDurationMsAttribute = new()
			{
				name = "randPosDurationMs",
				defaultValue = 0,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _randPosLerpFactorAttribute = new()
			{
				name = "randPosLerpFactor",
				defaultValue = 0,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosMinXAttribute = new()
			{
				name = "sineWavePosMinX",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosMinYAttribute = new()
			{
				name = "sineWavePosMinY",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosMaxXAttribute = new()
			{
				name = "sineWavePosMaxX",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosMaxYAttribute = new()
			{
				name = "sineWavePosMaxY",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosXOffsetAttribute = new()
			{
				name = "sineWavePosXOffset",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosYOffsetAttribute = new()
			{
				name = "sineWavePosYOffset",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosXFrequencyAttribute = new()
			{
				name = "sineWavePosXFrequency",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWavePosYFrequencyAttribute = new()
			{
				name = "sineWavePosYFrequency",
				defaultValue = 0f,
				restriction = new UxmlValueBounds() {excludeMin = false, excludeMax = false, min = "-999999", max = "999999"},
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var se = (AnimatedImageElement) ve;
				se.rotationsPerSecond = _rotationsPerSecondAttribute.GetValueFromBag(bag, cc);
				se.randPosMinX = _randPosMinXAttribute.GetValueFromBag(bag, cc);
				se.randPosMinY = _randPosMinYAttribute.GetValueFromBag(bag, cc);
				se.randPosMaxX = _randPosMaxXAttribute.GetValueFromBag(bag, cc);
				se.randPosMaxY = _randPosMaxYAttribute.GetValueFromBag(bag, cc);
				se.randPosDurationMs = _randPosDurationMsAttribute.GetValueFromBag(bag, cc);
				se.randPosLerpFactor = _randPosLerpFactorAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosMinX = _sineWavePosMinXAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosMinY = _sineWavePosMinYAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosMaxX = _sineWavePosMaxXAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosMaxY = _sineWavePosMaxYAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosXOffset = _sineWavePosXOffsetAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosYOffset = _sineWavePosYOffsetAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosXFrequency = _sineWavePosXFrequencyAttribute.GetValueFromBag(bag, cc);
				se.sineWavePosYFrequency = _sineWavePosYFrequencyAttribute.GetValueFromBag(bag, cc);
				
				if (se.rotationsPerSecond != 0)
				{
					se.AnimateRotation();
				}

				if (se.randPosMinX != 0 || se.randPosMinY != 0 || se.randPosMaxX != 0 || se.randPosMaxY != 0)
				{
					se.AnimateRandomPosition();
				}
				
				if (se.sineWavePosMinX != 0 || se.sineWavePosMinY != 0 || se.sineWavePosMaxX != 0 || se.sineWavePosMaxY != 0)
				{
					se.AnimateSineWavePosition();
				}
			}
		}
	}
}