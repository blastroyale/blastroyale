using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to change the position.
	///
	/// <see cref="UITPositionBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UITPositionClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UITPositionBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UITPositionBehaviour>.Create(graph, _template);
		}
	}
}