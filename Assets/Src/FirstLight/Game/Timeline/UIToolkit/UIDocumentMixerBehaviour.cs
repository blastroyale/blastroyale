using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A mixer for mixing / blending together UIT clips (behaviours).
	/// </summary>
	[Serializable]
	public class UIDocumentMixerBehaviour : PlayableBehaviour
	{
		public List<VisualElement> Elements;

		public override void OnGraphStart(Playable playable)
		{
			// Initialize clips
			for (int i = 0; i < playable.GetInputCount(); i++)
			{
				var playableInput = (ScriptPlayable<UIDocumentBehaviour>) playable.GetInput(i);
				var behaviour = playableInput.GetBehaviour();

				switch (behaviour)
				{
					case UIDocumentClassBehaviour cls:
						// Class behaviour handles it's own logic - doesn't use a mixer
						cls.Elements = Elements;
						break;
				}
			}
		}

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			var position = new Vector2();
			var rotation = 0f;
			var scale = new Vector2(1f, 1f);
			var opacity = 1f;

			for (int i = 0; i < playable.GetInputCount(); i++)
			{
				var playableInput = (ScriptPlayable<UIDocumentBehaviour>) playable.GetInput(i);
				var behaviour = playableInput.GetBehaviour();

				switch (behaviour)
				{
					case UIDocumentPositionBehaviour trs:
						position += trs.Position * playable.GetInputWeight(i);
						break;
					case UIDocumentScaleBehaviour scl:
						scale += scl.Scale * playable.GetInputWeight(i);
						break;
					case UIDocumentRotationBehaviour rot:
						rotation += rot.Rotation * playable.GetInputWeight(i);
						break;
					case UIDocumentOpacityBehaviour op:
						opacity += op.Opacity * playable.GetInputWeight(i);
						break;
				}
			}

			foreach (var e in Elements)
			{
				e.transform.position = position;
				e.transform.scale = Vector3.one * scale;
				e.transform.rotation = Quaternion.Euler(0, 0, rotation);
				e.style.opacity = opacity;
			}
		}
	}
}