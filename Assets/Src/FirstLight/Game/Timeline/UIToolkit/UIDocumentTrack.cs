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
			var root = go.GetComponentInParent<UIDocument>().rootVisualElement;
			var query = root.Query();
			var results = new List<VisualElement>(1);

			var path = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < path.Length; i++)
			{
				var part = path[i];

				if (part.StartsWith('#'))
				{
					if (part.IndexOf('#') != part.LastIndexOf('#'))
					{
						Debug.LogError($"Invalid pattern (only one Name(#) selector is allowed per part): {part}");
						return results;
					}

					query.Name(part.Replace("#", ""));
				}
				else if (part.StartsWith('.'))
				{
					if (part.Contains('#'))
					{
						Debug.LogError($"Invalid pattern (no mixing of Names(#) and Classes(#) in a part). Did you forget a space?: {part}");
						return results;
					}

					var classes = part.Split('.', StringSplitOptions.RemoveEmptyEntries);
					foreach (var @class in classes)
					{
						query.Class(@class);
					}
				}
				else
				{
					Debug.LogError($"Invalid pattern (only Name(#) and Class(.) is allowed): {part}");
				}

				if (i < path.Length - 1)
				{
					query = query.Children<VisualElement>();
				}
			}

			return query.Build().ToList();
		}
	}
}