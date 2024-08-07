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
		public float rotationsPerSecond { get; set; }
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
		
		private float sineWaveScaleMin { get; set; }
		private float sineWaveScaleMax { get; set; }
		private float sineWaveScaleFrequency { get; set; }

		private ValueAnimation<float> _rotationTween;
		private ValueAnimation<float> _randPosTween;
		private ValueAnimation<float> _sinewavePosTween;
		private ValueAnimation<float> _sinewaveScaleTween;

		private void AnimateRotation()
		{
			if (_rotationTween != null && _rotationTween.isRunning)
			{
				_rotationTween.Stop();
				_rotationTween.Recycle();
			}

			_rotationTween = experimental.animation.Start(0f, 1f, (int) (1000 / rotationsPerSecond),
														  (ve, percent) =>
														  {
															  ve.transform.rotation =
																  Quaternion.Euler(0, 0, 360 * percent);
														  });

			_rotationTween.Ease(Easing.Linear);
			_rotationTween.KeepAlive();
			_rotationTween.OnCompleted(() => { _rotationTween.Start(); });
		}

		private void AnimateRandomPosition()
		{
			if (_randPosTween != null && _randPosTween.isRunning)
			{
				_randPosTween.Stop();
				_randPosTween.Recycle();
			}

			var randomPos = new Vector2(Random.Range(randPosMinX, randPosMaxX), Random.Range(randPosMinY, randPosMaxY));

			_randPosTween = experimental.animation.Start(0f, 1f, randPosDurationMs, (ve, percent) =>
			{
				var lerpNewX = Mathf.Lerp(ve.transform.position.x, randomPos.x, percent / randPosLerpFactor);
				var lerpNewY = Mathf.Lerp(ve.transform.position.y, randomPos.y, percent / randPosLerpFactor);
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
				_sinewavePosTween.Stop();
				_sinewavePosTween.Recycle();
			}

			_sinewavePosTween = experimental.animation.Start(0f, 1f, 9999999, (ve, percent) =>
			{
				var sinX = Mathf.Sin((Time.realtimeSinceStartup * sineWavePosXFrequency) + sineWavePosXOffset);
				var sinY = Mathf.Sin((Time.realtimeSinceStartup * sineWavePosYFrequency) + sineWavePosYOffset);
				var normX = (sinX - -1) / (1 - -1);
				var normY = (sinY - -1) / (1 - -1);
				var lerpNewX = Mathf.Lerp(sineWavePosMinX, sineWavePosMaxX, normX);
				var lerpNewY = Mathf.Lerp(sineWavePosMinY, sineWavePosMaxY, normY);

				ve.transform.position = new Vector3(lerpNewX, lerpNewY, 0);
			});

			_sinewavePosTween.Ease(Easing.Linear);
			_sinewavePosTween.KeepAlive();
			_sinewavePosTween.OnCompleted(() => { _sinewavePosTween.Start(); });
		}
		
		private void AnimateSineWaveScale()
		{
			if (_sinewaveScaleTween != null && _sinewaveScaleTween.isRunning)
			{
				_sinewaveScaleTween.Stop();
				_sinewaveScaleTween.Recycle();
			}

			_sinewaveScaleTween = experimental.animation.Start(0f, 1f, 9999999, (ve, percent) =>
			{
				var sinX = Mathf.Sin(Time.realtimeSinceStartup * sineWaveScaleFrequency);
				var normX = (sinX - -1) / (1 - -1); // Wat
				var lerpNewX = Mathf.Lerp(sineWaveScaleMin, sineWaveScaleMax, normX);

				ve.transform.scale = new Vector3(lerpNewX, lerpNewX, lerpNewX);
			});

			_sinewaveScaleTween.Ease(Easing.Linear);
			_sinewaveScaleTween.KeepAlive();
			_sinewaveScaleTween.OnCompleted(() => { _sinewaveScaleTween.Start(); });
		}

		public new class UxmlFactory : UxmlFactory<AnimatedImageElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlFloatAttributeDescription _rotationsPerSecondAttribute = new()
			{
				name = "rotations-per-second",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _randPosMinXAttribute = new()
			{
				name = "rand-pos-min-x",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _randPosMinYAttribute = new()
			{
				name = "rand-pos-min-x",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _randPosMaxXAttribute = new()
			{
				name = "rand-pos-max-x",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _randPosMaxYAttribute = new()
			{
				name = "rand-pos-max-y",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlIntAttributeDescription _randPosDurationMsAttribute = new()
			{
				name = "rand-pos-duration-ms",
				defaultValue = 0,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _randPosLerpFactorAttribute = new()
			{
				name = "rand-pos-lerp-factor",
				defaultValue = 0,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosMinXAttribute = new()
			{
				name = "sine-wave-pos-min-x",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosMinYAttribute = new()
			{
				name = "sine-wave-pos-min-y",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosMaxXAttribute = new()
			{
				name = "sine-wave-pos-max-x",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosMaxYAttribute = new()
			{
				name = "sine-wave-pos-max-y",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosXOffsetAttribute = new()
			{
				name = "sine-wave-pos-x-offset",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosYOffsetAttribute = new()
			{
				name = "sine-wave-pos-y-offset",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosXFrequencyAttribute = new()
			{
				name = "sine-wave-pos-x-frequency",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};

			private readonly UxmlFloatAttributeDescription _sineWavePosYFrequencyAttribute = new()
			{
				name = "sine-wave-pos-y-frequency",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWaveScaleMinAttribute = new()
			{
				name = "sine-wave-scale-min",
				defaultValue = 1f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWaveScaleMaxAttribute = new()
			{
				name = "sine-wave-scale-max",
				defaultValue = 1f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
				use = UxmlAttributeDescription.Use.Required
			};
			
			private readonly UxmlFloatAttributeDescription _sineWaveScaleFrequencyAttribute = new()
			{
				name = "sine-wave-scale-frequency",
				defaultValue = 0f,
				restriction = new UxmlValueBounds()
					{excludeMin = false, excludeMax = false},
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
				se.sineWaveScaleMin = _sineWaveScaleMinAttribute.GetValueFromBag(bag, cc);
				se.sineWaveScaleMax = _sineWaveScaleMaxAttribute.GetValueFromBag(bag, cc);
				se.sineWaveScaleFrequency = _sineWaveScaleFrequencyAttribute.GetValueFromBag(bag, cc);

				if (se.rotationsPerSecond != 0)
				{
					se.AnimateRotation();
				}

				if (se.randPosMinX != 0 || se.randPosMinY != 0 || se.randPosMaxX != 0 || se.randPosMaxY != 0)
				{
					se.AnimateRandomPosition();
				}

				if (se.sineWavePosMinX != 0 || se.sineWavePosMinY != 0 || se.sineWavePosMaxX != 0 ||
					se.sineWavePosMaxY != 0)
				{
					se.AnimateSineWavePosition();
				}
				
				// ReSharper disable twice CompareOfFloatsByEqualityOperator
				if (se.sineWaveScaleMin != 1f || se.sineWaveScaleMax != 1f || se.sineWaveScaleFrequency != 0)
				{
					se.AnimateSineWaveScale();
				}
			}
		}
	}
}