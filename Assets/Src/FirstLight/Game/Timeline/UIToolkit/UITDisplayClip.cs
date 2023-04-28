using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to set a VisualElement ti Display.None or Display.Flex.
	///
	/// <see cref="UITClassBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UITDisplayClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UITDisplayBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.None;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UITDisplayBehaviour>.Create(graph, _template);
		}
	}
}