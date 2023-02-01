using System;
using FirstLight.Game.Utils;
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
	 TrackClipType(typeof(UIDocumentRotationClip)), TrackClipType(typeof(UIDocumentClassClip))]
	[TrackColor(0.259f, 0.529f, 0.961f)]
	public class UIDocumentTrack : TrackAsset, ILayerable
	{
		[SerializeField] private string _elementName;

		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			var mixer = ScriptPlayable<UIDocumentMixerBehaviour>.Create(graph, inputCount);
			// TODO: Not great, would be better if it got the document from the binding. But then it's less reusable. Not sure. Will leave like this for now
			mixer.GetBehaviour().Element = go.GetComponent<UIDocument>().rootVisualElement.Q(_elementName).Required();
			return mixer;
		}

		public Playable CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			return Playable.Null;
		}
	}
}