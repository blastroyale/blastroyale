using System;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableAsset"/> for enabling or disabling GameObjects on the timeline
	/// </remarks>
	public class GameObjectAsset : PlayableAssetBase<GameObjectBehaviour>
	{
		public ExposedReference<GameObject> Reference;

		protected override ScriptPlayable<GameObjectBehaviour> OnCreated(PlayableGraph graph, GameObject owner)
		{
			var playable = base.OnCreated(graph, owner);
			var obj = Reference.Resolve(graph.GetResolver());

			if (obj == null)
			{
				throw new InvalidOperationException($"The reference object {obj} is not yet referenced");
			}
			
			if (!Application.isPlaying)
			{
				return playable;
			}
			
			playable.GetBehaviour().GameObject = obj;

			return playable;
		}
	}
}