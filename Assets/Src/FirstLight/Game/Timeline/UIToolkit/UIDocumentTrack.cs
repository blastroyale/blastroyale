using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// A track supporting animation for UIDocuments / UIToolkit. A single track is responsible for a single element.
	/// </summary>
	[Serializable]
	[TrackClipType(typeof(UIDocumentScaleClip)), TrackClipType(typeof(UIDocumentPositionClip)),
	 TrackClipType(typeof(UIDocumentRotationClip)), TrackClipType(typeof(UIDocumentClassClip)),
	 TrackClipType(typeof(UIDocumentOpacityClip))]
	[TrackColor(0.259f, 0.529f, 0.961f)]
	public class UIDocumentTrack : TrackAsset, ILayerable
	{
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			var mixer = ScriptPlayable<UIDocumentMixerBehaviour>.Create(graph, inputCount);
			mixer.GetBehaviour().Element = GetQuoteUnquoteBoundElement(go);
			return mixer;
		}

		public Playable CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			return Playable.Null;
		}

		private VisualElement GetQuoteUnquoteBoundElement(GameObject go)
		{
			var root = go.GetComponent<UIDocument>().rootVisualElement;
			var path = name.Split('#');

			var ve = root;
			foreach (var elementName in path)
			{
				ve = ve.Q(elementName);
			}

			return ve;
		}
	}
}