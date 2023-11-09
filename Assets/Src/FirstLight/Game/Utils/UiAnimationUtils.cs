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
		public static IValueAnimation AnimatePing(this VisualElement element, float amount = 1.4f, int duration = 150, bool repeat = false)
		{
			var anim = element.experimental.animation.Scale(amount, duration).OnCompleted(() =>
			{
				element.experimental.animation.Scale(1f, duration).OnCompleted(() =>
				{
					if (repeat)
					{
						element.AnimatePing(amount, duration, repeat);
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
				currentAngle += angleDelta;
				element.style.rotate = new StyleRotate(new Rotate(new Angle(currentAngle)));
			}).Every(delay);
		}
	}
}