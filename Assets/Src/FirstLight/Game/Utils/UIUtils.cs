using System;
using System.Linq;
using System.Security.Cryptography;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

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
			var viewportPoint = element.worldBound.center / root.worldBound.size;

			if (invertX)
			{
				viewportPoint.x = 1f - viewportPoint.x;
			}

			if (invertY)
			{
				viewportPoint.y = 1f - viewportPoint.y;
			}

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

		/// <summary>
		/// Opens a tooltip for <paramref name="element"/> (bottom left).
		/// </summary>
		public static void OpenTooltip(this VisualElement element, VisualElement root, string content, int offsetX = 0,
									   int offsetY = 0)

		{
			var blocker = new VisualElement();
			root.Add(blocker);
			blocker.AddToClassList("tooltip-holder");
			blocker.RegisterCallback<ClickEvent, VisualElement>((_, ve) => { ve.RemoveFromHierarchy(); }, blocker,
				TrickleDown.TrickleDown);

			var tooltip = new Label(content);

			tooltip.AddToClassList("tooltip");
			tooltip.RegisterCallback<AttachToPanelEvent>(ev =>
			{
				var pos = element.worldBound.position;
				var rootBound = root.worldBound;

				pos.x -= rootBound.width - offsetX;
				pos.y += element.worldBound.height - offsetY;

				tooltip.transform.position = pos;
			});

			tooltip.experimental.animation
				.Start(0f, 1f, 200, (ve, val) => ve.style.opacity = val)
				.Ease(Easing.Linear);

			blocker.Add(tooltip);
		}
	}
}