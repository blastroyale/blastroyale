using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to change the opacity.
	///
	/// <see cref="UIDocumentOpacityBehaviour"/>
	/// </summary>
	[Serializable]
	public class UIDocumentOpacityClip : PlayableAsset, ITimelineClipAsset
	{
		public UIDocumentOpacityBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentOpacityBehaviour>.Create(graph, _template);
		}
	}
}