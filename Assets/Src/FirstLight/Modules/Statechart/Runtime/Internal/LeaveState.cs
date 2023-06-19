using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="ILeaveState"/>
	internal class LeaveState : StateInternal, ILeaveState
	{
		private readonly EnterExitDefaultHandler _enterExitHandler;

		internal ITransitionInternal LeaveTransition { get; private set; }

		public LeaveState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_enterExitHandler = new EnterExitDefaultHandler(this);
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_enterExitHandler.Enter();
		}

		/// <inheritdoc />
		public override void Exit()
		{
			// Do nothing on the final state
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if(LeaveTransition?.TargetState == null)
			{
				throw new MissingMemberException($"The leave state {Name} is not pointing to any state");
			}
			
			if(LeaveTransition.TargetState.RegionLayer != RegionLayer - 1)
			{
				throw new InvalidOperationException($"The leave state {Name} is not pointing to a state in the above region layer");
			}
#endif
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			return null;
		}

		/// <inheritdoc />
		public void OnEnter(Action action)
		{
			_enterExitHandler.OnEnter(action);
		}

		public void OnEnterAsync(Func<Task> task)
		{
			_enterExitHandler.OnEnterAsync(task);
		}

		/// <inheritdoc />
		public ITransition Transition()
		{
			if (LeaveTransition != null)
			{
				throw new InvalidOperationException($"State {Name} already has a transition defined");
			}

			LeaveTransition = new Transition();

			return LeaveTransition;
		}
	}
}
