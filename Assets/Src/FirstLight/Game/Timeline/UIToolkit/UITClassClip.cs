using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to enable a class on a VisualElement.
	///
	/// <see cref="UITClassBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UITClassClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UITClassBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.None;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UITClassBehaviour>.Create(graph, _template);
		}
	}
}