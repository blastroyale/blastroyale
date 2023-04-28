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
	/// <see cref="UIDocumentRotationBehaviour"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentRotationClip : PlayableAsset, ITimelineClipAsset
	{
		[InlineProperty, HideLabel] public UIDocumentRotationBehaviour _template = new();

		public ClipCaps clipCaps => ClipCaps.All;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			return ScriptPlayable<UIDocumentRotationBehaviour>.Create(graph, _template);
		}
	}
}