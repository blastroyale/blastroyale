using System;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helper methods for UI Toolkit elements / documents.
	/// </summary>
	public static class UIUtils
	{
		/// <summary>
		/// Throws an exception if the <paramref name="visualElement"/> is null.
		/// </summary>
		public static T Required<T>(this T visualElement) where T : VisualElement
		{
			if (visualElement == null)
			{
				throw new NullReferenceException("VisualElement should not be null!");
			}

			return visualElement;
		}

		/// <summary>
		/// Sets up pointer down SFX callbacks for all elements with the "sfx-click" class.
		/// </summary>
		public static void SetupClicks(this VisualElement root, IGameServices gameServices)
		{
			foreach (var ve in root.Query(null, UIConstants.SFX_CLICK_FORWARDS).Build())
			{
				ve.RegisterCallback<PointerDownEvent, IGameServices>(
					(_, service) => { service.AudioFxService.PlayClip2D(AudioId.ButtonClickForward); },
					gameServices,
					TrickleDown.TrickleDown);
			}

			foreach (var ve in root.Query(null, UIConstants.SFX_CLICK_BACKWARDS).Build())
			{
				ve.RegisterCallback<PointerDownEvent, IGameServices>(
					(_, service) => { service.AudioFxService.PlayClip2D(AudioId.ButtonClickBackward); },
					gameServices,
					TrickleDown.TrickleDown);
			}
		}

		/// <summary>
		/// Gets the position (center of content rect) of the <paramref name="element"/>, in screen coordinates.
		/// TODO: There has to be a better way to do this, without using the camera
		/// </summary>
		public static Vector2 GetPositionOnScreen(this VisualElement element, VisualElement root)
		{
			var viewportPoint = element.worldBound.center / root.worldBound.size;
			viewportPoint.y = 1 - viewportPoint.y;

			return Camera.main.ViewportToScreenPoint(viewportPoint);
		}

		/// <summary>
		/// Removes all BEM modifiers from the class list.
		/// </summary>
		public static void RemoveModifiers(this VisualElement element, bool skipAnimations = true)
		{
			var classes = element.GetClasses().ToList();

			foreach (var clazz in classes)
			{
				if(skipAnimations && clazz.StartsWith("anim")) continue;

				if (clazz.Contains("--"))
				{
					element.RemoveFromClassList(clazz);
				}
			}
		}
	}
}