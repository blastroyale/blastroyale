using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Debug)]
	public class WorldDebugPresenter : UIPresenter
	{
		public static WorldDebugPresenter Instance;

		public WorldDebugPresenter()
		{
			Instance = this;
		}

		private List<FollowEntry> _following = new ();

		public class FollowEntry
		{
			public VisualElement Element;
			public Transform Following;
		}

		protected override void QueryElements()
		{
		}

		public FollowEntry DebugElement(Transform follow, VisualElement element)
		{
			Root.Add(element);
			var entry = new FollowEntry()
			{
				Element = element,
				Following = follow
			};
			_following.Add(entry);
			return entry;
		}

		public void DebugText(Transform follow, string text, float duration)
		{
			var element = new LabelOutlined(text).AddClass("game-label");
			var entry = DebugElement(follow, element);
			UniTask.Void(async () =>
			{
				await UniTask.WaitForSeconds(duration);
				_following.Remove(entry);
			});
		}

		private void Update()
		{
			var toremove = new List<FollowEntry>();
			foreach (var fe in _following)
			{
				if (!fe.Following)
				{
					toremove.Add(fe);
					continue;
				}

				if (!IsVisible(fe.Following.position))
				{
					fe.Element.SetDisplay(false);
					continue;
				}

				fe.Element.SetDisplay(true);
				fe.Element.SetPositionBasedOnWorldPosition(fe.Following.position);
			}

			foreach (var followEntry in toremove)
			{
				Root.Remove(followEntry.Element);
				_following.Remove(followEntry);
			}
		}

		public bool IsVisible(Vector3 position)
		{
			{
				//Check Visibility

				var screenPos = FLGCamera.Instance.MainCamera.WorldToScreenPoint(position);
				var onScreen = screenPos.x > 0f && screenPos.x < Screen.width && screenPos.y > 0f && screenPos.y < Screen.height;
				return onScreen;
			}
		}
	}
}