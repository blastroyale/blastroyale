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
	/// <see cref="UIDocumentClassBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentClassClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UIDocumentClassBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.None;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentClassBehaviour>.Create(graph, _template);
		}
	}
}