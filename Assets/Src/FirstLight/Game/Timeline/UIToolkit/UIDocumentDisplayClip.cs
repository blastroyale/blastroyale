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
	/// <see cref="UIDocumentClassBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentDisplayClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UIDocumentDisplayBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.None;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentDisplayBehaviour>.Create(graph, _template);
		}
	}
}