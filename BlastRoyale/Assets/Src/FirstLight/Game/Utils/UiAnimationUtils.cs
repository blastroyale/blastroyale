using System;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Utils
{
	public static class UiAnimationUtils
	{
		/// <summary>
		/// Animates the scale up and then back down to 1
		/// </summary>
		public static IValueAnimation AnimatePing(this VisualElement element, float amount = 1.4f, int duration = 150, bool repeat = false, long delay = 0)
		{
			var toScale = element.experimental.animation.Scale(amount, duration);
			ValueAnimation<float> backToOne = null;
			toScale.OnCompleted(() =>
			{
				if (backToOne == null)
				{
					backToOne = element.experimental.animation.Scale(1f, duration);
					if (repeat)
					{
						backToOne.KeepAlive();
						backToOne.OnCompleted(() =>
						{
							if (delay > 0)
							{
								element.schedule.Execute(() =>
								{
									toScale.Start();
								}).ExecuteLater(delay);
								return;
							}

							toScale.Start();
						});
					}
				}

				backToOne.Start();
			});
			if (repeat)
			{
				toScale.KeepAlive();
			}

			toScale.Start();
			return toScale;
		}

		/// <summary>
		/// Animates the scale up and then back down to 1
		/// </summary>
		public static IValueAnimation AnimateTransform(this VisualElement element, Vector3 offset, int duration = 150, bool repeat = false, Func<float, float> easing = null)
		{
			var anim = element.experimental.animation.Position(offset, duration).OnCompleted(() =>
			{
				element.experimental.animation.Position(Vector3.zero, duration).OnCompleted(() =>
				{
					if (repeat)
					{
						element.AnimateTransform(offset, duration, true);
					}
				}).Ease(easing ?? Easing.OutQuad).Start();
			});

			if (easing != null)
			{
				anim.easingCurve = easing;
			}

			return anim;
		}

		/// <summary>
		/// Animates the opacity from <paramref name="fromAmount"/> up to <paramref name="toAmount"/> and back to <paramref name="fromAmount"/>;
		/// </summary>
		public static IValueAnimation AnimatePingOpacity(this VisualElement element, float fromAmount = 0f, float toAmount = 1f, int duration = 150, bool repeat = false)
		{
			var from = new StyleValues
			{
				opacity = fromAmount
			};

			var to = new StyleValues
			{
				opacity = toAmount
			};

			var anim = element.experimental.animation.Start(from, to, duration).OnCompleted(() =>
			{
				element.experimental.animation.Start(to, from, duration).OnCompleted(() =>
				{
					if (repeat)
					{
						element.AnimatePingOpacity(fromAmount, toAmount, duration, repeat);
					}
				}).Start();
			});
			anim.Start();
			return anim;
		}

		/// <summary>
		/// Rotates element slowly. Can be used to let elements rotating forever.
		/// </summary>
		public static void AddRotatingEffect(this VisualElement element, float angleDelta, int delay)
		{
			var currentAngle = element.style.rotate.value.angle.value;
			element.schedule.Execute(() =>
			{
				currentAngle += angleDelta * Time.deltaTime;
				element.style.rotate = new StyleRotate(new Rotate(new Angle(currentAngle)));
			}).Every(delay);
		}
	}
}