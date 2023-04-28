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
	/// <see cref="UIDocumentClassBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentVisibilityClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UIDocumentVisibilityBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.None;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentVisibilityBehaviour>.Create(graph, _template);
		}
	}
}