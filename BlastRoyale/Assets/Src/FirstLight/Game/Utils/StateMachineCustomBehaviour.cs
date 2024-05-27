using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Custom implementation of a state machine behaviour to allow custom state invoke calls
	/// </summary>
	public class StateMachineCustomBehaviour : StateMachineBehaviour
	{
		/// <inheritdoc cref="OnStateEnter"/>
		public readonly UnityEvent<Animator, AnimatorStateInfo, int> OnStateEnterEvent = new UnityEvent<Animator, AnimatorStateInfo, int>();
		/// <inheritdoc cref="OnStateExit"/>
		public readonly UnityEvent<Animator, AnimatorStateInfo, int> OnStateExitEvent = new UnityEvent<Animator, AnimatorStateInfo, int>();
		
		/// <inheritdoc />
		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateEnter(animator, stateInfo, layerIndex);
			
			OnStateEnterEvent.Invoke(animator, stateInfo, layerIndex);
		}

		/// <inheritdoc />
		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateExit(animator, stateInfo, layerIndex);
			
			OnStateExitEvent.Invoke(animator, stateInfo, layerIndex);
		}
	}
}