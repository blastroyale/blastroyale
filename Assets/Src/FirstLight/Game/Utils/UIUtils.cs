using System;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using I2.Loc;
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
		public static Vector2 GetPositionOnScreen(this VisualElement element, VisualElement root, bool invertY = true,
												  bool invertX = false)
		{
			if (!element.worldBound.Overlaps(root.worldBound))
			{
				throw new Exception("Element out of bounds");
			}

			var viewportPoint = element.worldBound.center / root.worldBound.size;

			if (invertX)
			{
				viewportPoint.x = 1f - viewportPoint.x;
			}

			if (invertY)
			{
				viewportPoint.y = 1f - viewportPoint.y;
			}

			var screenPoint = Camera.main.ViewportToScreenPoint(viewportPoint);

			// if viewportPoint.x = 1f ViewportToScreenPoint will return width as x, which should be width-1
			screenPoint.x = Math.Max(screenPoint.x, 0);
			screenPoint.y = Math.Max(screenPoint.y, 0);
			screenPoint.x = Math.Min(screenPoint.x, Screen.width - 1);
			screenPoint.y = Math.Min(screenPoint.y, Screen.height - 1);

			return screenPoint;
		}

		/// <summary>
		/// Removes all BEM modifiers from the class list.
		/// </summary>
		public static void RemoveModifiers(this VisualElement element, bool skipAnimations = true)
		{
			var classes = element.GetClasses().ToList();

			foreach (var clazz in classes)
			{
				if (skipAnimations && clazz.StartsWith("anim")) continue;

				if (clazz.Contains("--"))
				{
					element.RemoveFromClassList(clazz);
				}
			}
		}

		/// <summary>
		/// Removes all sprite classes (the auto generated ones) from the element.
		/// </summary>
		/// <param name="element"></param>
		public static void RemoveSpriteClasses(this VisualElement element)
		{
			var classes = element.GetClasses().ToList();

			foreach (var clazz in classes)
			{
				if (clazz.StartsWith("sprite-"))
				{
					element.RemoveFromClassList(clazz);
				}
			}
		}

		/// <summary>
		/// Localizes a string, assuming it's a key, and displays the key if localization isn't found.
		/// </summary>
		public static string LocalizeKey(this string key)
		{
			return LocalizationManager.TryGetTranslation(key, out var translation)
				? translation
				: $"#{key}#";
		}

		/// <summary>
		/// Disables the scrollbar visibility on a ListView
		/// </summary>
		public static void DisableScrollbars(this ListView listView)
		{
			var scroller = listView.Q<ScrollView>();

			scroller.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			scroller.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
		}

		/// <summary>
		/// Checks if this element is attached to a panel.
		/// </summary>
		public static bool IsAttached(this VisualElement element)
		{
			return element.panel != null;
		}
	}
}