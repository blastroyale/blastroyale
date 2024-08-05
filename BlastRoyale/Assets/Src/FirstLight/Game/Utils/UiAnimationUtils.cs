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
		public static ValueAnimation<float> AnimatePing(this VisualElement element, float amount = 1.4f, int duration = 150, int delay = 0)
		{
			var easing = new Func<float, float>(Easing.Linear);
			var totalTime = (duration * 2) + delay;
			return element.experimental.animation.Start(0f, totalTime, totalTime, (el, f) =>
			{
				var extraScale = amount - 1;
				if (f <= duration) // growing
				{
					var pct = easing(f / duration);
					var size = 1 + extraScale * pct;
					el.transform.scale = new Vector3(size, size, size);
					return;
				}

				if (f <= duration * 2) // shrinking
				{
					var pct = 1 - easing((f - duration) / duration);
					var size = 1 + extraScale * pct;
					el.transform.scale = new Vector3(size, size, size);
					return;
				}
			});
		}

		/// <summary>
		/// Animates the scale up and then back down to 1
		/// <returns>Cancel callback</returns>
		/// </summary>
		public static Action AnimatePingRepeating(this VisualElement element, float amount = 1.4f, int duration = 150, int delay = 0)
		{
			var cancelled = false;
			var anim = AnimatePing(element, amount, duration, delay);

			anim.KeepAlive();
			anim.OnCompleted(() =>
			{
				if (cancelled)
				{
					anim.Recycle();
					return;
				}
				anim.Start();
			});
			return () =>
			{
				if (cancelled) return;
				cancelled = true;
				element.transform.scale = Vector3.one;
				anim.Stop();
			};
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
		public static IValueAnimation AnimatePingOpacity(this VisualElement element, float fromAmount = 0f, float toAmount = 1f, int duration = 150, bool repeat = false, bool rewind = true)
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
				if (!rewind) return;
				
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

		public static IVisualElementScheduledItem SetTimer(this TextElement element, Func<DateTime> endTimeGetter, string prefix, Action<TextElement> finishCallback = null)
		{
			return element.schedule.Execute(() =>
			{
				var timeLeft = endTimeGetter() - DateTime.UtcNow;
				if (timeLeft.TotalSeconds < 0)
				{
					finishCallback?.Invoke(element);
					return;
				}

				element.text = $"{prefix}{timeLeft.Display(showSeconds: true, showDays: false).ToLowerInvariant()}";
			}).Every(1000);
		}
	}
}