using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views.UITK;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
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
		[HideInCallstack]
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
			foreach (var ve in root.Query(null, UIService.UIService.SFX_CLICK_FORWARDS).Build())
			{
				ve.RegisterCallback<PointerDownEvent, IGameServices>(
					(_, service) => { service.AudioFxService.PlayClip2D(AudioId.ButtonClickForward); },
					gameServices,
					TrickleDown.TrickleDown);
			}

			foreach (var ve in root.Query(null, UIService.UIService.SFX_CLICK_BACKWARDS).Build())
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
		public static bool IsInScreen(this VisualElement element, VisualElement root)
		{
			return element.worldBound.Overlaps(root.worldBound);
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
				throw new Exception("Unable to get position in screen!");
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

			var screenPoint = FLGCamera.Instance.MainCamera.ViewportToScreenPoint(viewportPoint);

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
				: $"{key}";
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
		/// Returns the rarity style for a Battle Pass reward.
		/// </summary>
		public static string GetBPRarityStyle(GameId id)
		{
			const string USS_SPRITE_RARITY_MODIFIER = "sprite-home__pattern-rewardglow-";
			const string USS_SPRITE_RARITY_COMMON = USS_SPRITE_RARITY_MODIFIER + "common";
			const string USS_SPRITE_RARITY_UNCOMMON = USS_SPRITE_RARITY_MODIFIER + "uncommon";
			const string USS_SPRITE_RARITY_RARE = USS_SPRITE_RARITY_MODIFIER + "rare";
			const string USS_SPRITE_RARITY_EPIC = USS_SPRITE_RARITY_MODIFIER + "epic";
			const string USS_SPRITE_RARITY_LEGENDARY = USS_SPRITE_RARITY_MODIFIER + "legendary";

			return id switch
			{
				GameId.CoreCommon    => USS_SPRITE_RARITY_COMMON,
				GameId.CoreUncommon  => USS_SPRITE_RARITY_UNCOMMON,
				GameId.CoreRare      => USS_SPRITE_RARITY_RARE,
				GameId.CoreEpic      => USS_SPRITE_RARITY_EPIC,
				GameId.CoreLegendary => USS_SPRITE_RARITY_LEGENDARY,
				_                    => ""
			};
		}

		public static async UniTask<Sprite> LoadSprite(GameId id)
		{
			// TODO: This should be handled better.
			var services = MainInstaller.Resolve<IGameServices>();
			var sprite = await services.AssetResolverService.RequestAsset<GameId, Sprite>(id, instantiate: false);
			return sprite;
		}

		public static async UniTask SetSprite(GameId id, params VisualElement[] elements)
		{
			await SetSprite(LoadSprite(id), elements);
		}

		public static async UniTask SetSprite(UniTask<Sprite> fetchSpriteTask, params VisualElement[] elements)
		{
			foreach (var visualElement in elements)
			{
				if (visualElement == null || visualElement.panel == null || visualElement.panel.visualTree == null)
				{
					FLog.Warn($"Skipping nulling element {visualElement?.name} as its not valid");
					continue;
				}

				visualElement.style.backgroundImage = null;
			}

			var sprite = await fetchSpriteTask;
			foreach (var visualElement in elements)
			{
				if (visualElement == null || visualElement.panel == null || visualElement.panel.visualTree == null)
				{
					FLog.Warn($"Skipping updating element background element {visualElement?.name} as its not valid");
					continue;
				}

				visualElement.style.backgroundImage = new StyleBackground(sprite);
			}
		}

		/// <summary>
		/// Locks an element behind a level. unlockedCallback is triggered when this element isn't locked and is pressed.
		/// </summary>
		public static void LevelLock<TElement>(this TElement element,
											   UIPresenter presenter, VisualElement root, UnlockSystem unlockSystem,
											   Action unlockedCallback)
			where TElement : VisualElement
		{
			element.AttachView(presenter, out FameLockedView storeLockedView);
			storeLockedView.Init(unlockSystem, root, unlockedCallback);
		}

		/// <summary>
		/// Sets the PFP and level of the local player to this avatar element.
		/// </summary>
		public static void SetLocalPlayerData(this PlayerAvatarElement element, IGameDataProvider gameDataProvider, IGameServices gameServices)
		{
			element.SetLevel(gameDataProvider.PlayerDataProvider.Level.Value);

			var itemData = gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PROFILE_PICTURE);
			var spriteTask = gameServices.CollectionService.LoadCollectionItemSprite(itemData);

			element.LoadFromTask(spriteTask);
		}

		/// <summary>
		/// Get the position inside the Panel of a World Position
		/// Example: Damage numbers: get the position of the player in the world, call this function, and then use 
		/// the result to set the element inside the panel  
		/// </summary>
		public static Vector2 WorldPositionToPanel(IPanel panel, Vector3 worldPosition)
		{
			var flgCamera = FLGCamera.Instance.MainCamera;
			var screenPoint = flgCamera.WorldToScreenPoint(worldPosition);
			screenPoint.y = flgCamera.pixelHeight - screenPoint.y;
			return RuntimePanelUtils.ScreenToPanel(panel, screenPoint);
		}

		/// <summary>
		/// Set the transform.position of an UIToolkit element to be at the same place of a given position in the 3D world
		/// </summary>
		public static void SetPositionBasedOnWorldPosition(this VisualElement element, Vector3 worldPosition)
		{
			element.style.position = Position.Absolute;
			element.transform.position = WorldPositionToPanel(element.panel, worldPosition);
		}

		public static void SetPadding(this IStyle st, int padding)
		{
			st.paddingBottom = 0;
			st.paddingLeft = 0;
			st.paddingRight = 0;
			st.paddingTop = 0;
		}

		public static void SetMargin(this IStyle st, int padding)
		{
			st.marginBottom = 0;
			st.marginLeft = 0;
			st.marginRight = 0;
			st.marginTop = 0;
		}

		public static void ListenOnce<T>(this VisualElement visual, Action action) where T : EventBase<T>, new()
		{
			ListenOnce<T>(visual, (_) =>
			{
				action();
			});
		}

		public static void ListenOnce<T>(this VisualElement visual, EventCallback<T> action) where T : EventBase<T>, new()
		{
			EventCallback<T> wrapped = null;
			wrapped = @event =>
			{
				action(@event);
				visual.UnregisterCallback(wrapped);
			};
			visual.RegisterCallback(wrapped);
		}

		/// <summary>
		/// Copies the provided string to the system clipboard.
		/// </summary>
		public static void SaveToClipboard(string value)
		{
			var te = new TextEditor
			{
				text = value
			};
			te.SelectAll();
			te.Copy();
		}

		public static T AddClass<T>(this T visualElement, params string[] classes) where T : VisualElement
		{
			foreach (var @class in classes)
			{
				visualElement.AddToClassList(@class);
			}

			return visualElement;
		}

		public static void LoadTemplateAndBind(this VisualElement visualElement, string path)
		{
			var tree = Resources.Load<VisualTreeAsset>(path);
			tree.CloneTree(visualElement);
			visualElement.AssignElementResults(visualElement);
		}
		
		/*
		 * An Extension method that formats the given Label text to Cooldown/Expires from UTC now to given time in miliseconds  
		 */
		public static void ShowCooldown(this Label cooldownTimeLabel, DateTime cooldownRemaining, bool showClockSprite = false, Action cooldownExpiredCallback = null)
		{

			var cooldownRemainingSeconds = (cooldownRemaining - DateTime.UtcNow).TotalSeconds;
			
			cooldownTimeLabel.schedule.Execute(() =>
				{

					var currentCooldown = TimeSpan.FromSeconds(cooldownRemainingSeconds);
					
					cooldownTimeLabel.text = currentCooldown.ToDaysHourMinutesOrHourMinutesSeconds(true);
					
					if (showClockSprite)
					{
						cooldownTimeLabel.text = "<sprite name=\"ClockIcon\"> " + cooldownTimeLabel.text;
					}

					cooldownRemainingSeconds--;

					if (cooldownRemainingSeconds <= 0)
					{
						cooldownExpiredCallback?.Invoke();
					}
				})
				.Every(1000)
				.Until(() => cooldownRemainingSeconds <= 0);
		}
		

		public static VisualElement GetRoot(this VisualElement vs)
		{
			var p = vs;

			while (p.parent != null)
			{
				p = p.parent;
			}

			return p;
		}
	}
}