using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="ITransitionState"/>
	internal class TransitionState : StateInternal, ITransitionState
	{
		private ITransitionInternal _transition;
		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly EnterExitDefaultHandler _enterExitHandler;


		public TransitionState(string name, IStateFactoryInternal factory) : base(name, factory)
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
			_enterExitHandler.Exit();
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_transition?.TargetState == null)
			{
				throw new MissingMemberException($"Transition state {Name} doesn't have a transition defined, so nothing will happen");
			}

			if (_transition.TargetState.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}
#endif
		}

		/// <inheritdoc />
		public ITransition Transition()
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} already has a transition defined");
			}

			_transition = new Transition();

			return _transition;
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
		public void OnExit(Action action)
		{
			_enterExitHandler.OnExit(action);
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			return _transition;
		}
		
		public override Dictionary<string, object> CurrentState
		{
			get
			{
				var state = base.CurrentState;
				state.Add("TransitionTo", _transition.TargetState.Name);
				return state;
			}
		}
	}
}