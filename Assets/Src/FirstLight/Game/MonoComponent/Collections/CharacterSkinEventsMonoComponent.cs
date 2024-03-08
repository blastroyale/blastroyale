using System;
using JetBrains.Annotations;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Collections
{
	public class CharacterSkinEventsMonoComponent : MonoBehaviour
	{
		
		/// <summary>
		/// When the left foot steps / is on the ground.
		/// </summary>
		public event Action OnStepLeft;
		
		/// <summary>
		/// When the right foot steps / is on the ground.
		/// </summary>
		public event Action OnStepRight;

		[UsedImplicitly]
		private void StepLeft()
		{
			OnStepLeft?.Invoke();
		}

		[UsedImplicitly]
		private void StepRight()
		{
			OnStepRight?.Invoke();
		}
	}
}