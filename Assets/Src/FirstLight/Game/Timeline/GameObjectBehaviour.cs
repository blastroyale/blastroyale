using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableBehaviour"/> for enabling or disabling GameObjects on the timeline
	/// </remarks>
	[System.Serializable]
	public class GameObjectBehaviour : PlayableBehaviourBase
	{
		public bool EnableOnEnter = true;
		public bool EnableOnExit;
		public bool DisableOnEnter;
		public bool DisableOnExit;

		public GameObject GameObject { get; set; }

		/// <inheritdoc />
		protected override void OnEnter(Playable playable)
		{
			if (EnableOnEnter)
			{
				GameObject.SetActive(true);
			}
			else if (DisableOnEnter)
			{
				GameObject.SetActive(false);
			}
		}

		/// <inheritdoc />
		protected override void OnExit(Playable playable)
		{
			if (DisableOnExit)
			{
				GameObject.SetActive(false);
			}
			else if (EnableOnExit)
			{
				GameObject.SetActive(true);
			}
		}
	}
}