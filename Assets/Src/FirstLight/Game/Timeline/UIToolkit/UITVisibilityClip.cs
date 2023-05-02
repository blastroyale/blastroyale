using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to make a VisualElement visible or invisible.
	///
	/// <see cref="UITClassBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UITVisibilityClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UITVisibilityBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.None;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UITVisibilityBehaviour>.Create(graph, _template);
		}
	}
}