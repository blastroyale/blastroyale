using System;
using System.Collections.Generic;
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
			mixer.GetBehaviour().Elements = GetQuoteUnquoteBoundElements(go);
			return mixer;
		}

		public Playable CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			return Playable.Null;
		}

		private List<VisualElement> GetQuoteUnquoteBoundElements(GameObject go)
		{
			var root = go.GetComponent<UIDocument>().rootVisualElement;

			var elements = new List<VisualElement>(1);

			if (name.StartsWith("#"))
			{
				var path = name.Split('#', StringSplitOptions.RemoveEmptyEntries);

				var ve = root;
				foreach (var elementName in path)
				{
					ve = ve.Q(name: elementName);
				}

				elements.Add(ve);
			}
			else if (name.StartsWith("."))
			{
				root.Query(className: name.Replace(".", "")).Build().ToList(elements);
			}
			else
			{
				Debug.LogError("Invalid track / element name");
				return null;
			}

			if (elements.Count == 0)
			{
				Debug.LogError($"Could not find any elements matching the pattern: {name}");
			}

			return elements;
		}
	}
}