using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="IFinalState"/>
	internal class FinalState : StateInternal, IFinalState
	{
		private readonly EnterExitDefaultHandler _enterExitHandler;

		public FinalState(string name, IStateFactoryInternal factory) : base(name, factory)
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
			// Do nothing on the final state
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

	}
}