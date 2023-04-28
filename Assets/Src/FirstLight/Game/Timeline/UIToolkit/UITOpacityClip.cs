using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to change the opacity.
	///
	/// <see cref="UITOpacityBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UITOpacityClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UITOpacityBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UITOpacityBehaviour>.Create(graph, _template);
		}
	}
}