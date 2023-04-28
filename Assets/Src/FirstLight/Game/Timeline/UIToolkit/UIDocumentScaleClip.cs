using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A UI Toolkit timeline clip to change the scale.
	///
	/// <see cref="UIDocumentScaleBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentScaleClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UIDocumentScaleBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentScaleBehaviour>.Create(graph, _template);
		}
	}
}