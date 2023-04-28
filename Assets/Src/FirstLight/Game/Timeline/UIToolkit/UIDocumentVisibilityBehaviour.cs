using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// The behaviour / data of the visibility enable / disable.
	/// 
	/// <see cref="UIDocumentVisibilityClip"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentVisibilityBehaviour : UIDocumentBehaviour
	{
		public bool Visible;

		[HideInInspector] public List<VisualElement> Elements;

		public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
			if (Elements == null) return;
			foreach (var e in Elements)
			{
				e.visible = Visible;
			}
		}

		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
			if (Elements == null) return;
			foreach (var e in Elements)
			{
				e.visible = !Visible;
			}
		}
	}
}