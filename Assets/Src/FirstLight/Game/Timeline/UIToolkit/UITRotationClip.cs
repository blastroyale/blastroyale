using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to change the rotation.
	///
	/// <see cref="UITRotationBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UITRotationClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UITRotationBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UITRotationBehaviour>.Create(graph, _template);
		}
	}
}