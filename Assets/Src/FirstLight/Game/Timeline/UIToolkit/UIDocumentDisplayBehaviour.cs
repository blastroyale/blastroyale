using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// The behaviour / data of the display enable / disable (Flex / None).
	/// 
	/// <see cref="UIDocumentDisplayClip"/>
	/// </summary>
	[Serializable]
	[HideMonoScript]
	public class UIDocumentDisplayBehaviour : UIDocumentBehaviour
	{
		public bool Display;

		[HideInInspector] public List<VisualElement> Elements;

		public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
			if (Elements == null) return;
			foreach (var e in Elements)
			{
				e.style.display = Display ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
			if (Elements == null) return;
			foreach (var e in Elements)
			{
				e.style.display = !Display ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
	}
}