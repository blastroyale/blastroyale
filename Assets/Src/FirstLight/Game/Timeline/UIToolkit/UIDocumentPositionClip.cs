using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to change the position.
	///
	///  <see cref="UIDocumentPositionBehaviour"/>
	/// </summary>
	[Serializable]
	public class UIDocumentPositionClip : PlayableAsset, ITimelineClipAsset
	{
		public UIDocumentPositionBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentPositionBehaviour>.Create(graph, _template);
		}
	}
}