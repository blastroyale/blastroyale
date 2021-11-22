using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="INestState"/>
	internal class NestState : StateInternal, INestState
	{
		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly Dictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();
		
		private ITransitionInternal _transition;
		private IStateInternal _initialInnerState;
		private IStateInternal _currentInnerState;
		private IStateFactoryInternal _nestStateFactory;
		private bool _executeExit;
		private bool _executeFinal;
		
		public NestState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_currentInnerState = _initialInnerState;

			for(var i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			if (_executeExit)
			{
				_currentInnerState.Exit();
			}
			
			if (_executeFinal && !(_currentInnerState is FinalState) && !(_currentInnerState is LeaveState))
			{
				_nestStateFactory.FinalState?.Enter();
			}
			
			for(var i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_initialInnerState == null)
			{
				throw new MissingMemberException($"Nest state {Name} doesn't have a nested setup defined");
			}
			
			if (_executeFinal && _nestStateFactory.FinalState == null)
			{
				throw new MissingMemberException($"Nest state {Name} doesn't have a final state in his nested " +
				                                 $"setup and is marked to execute it's {nameof(IFinalState.OnEnter)} when completed");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}

			foreach (var eventTransition in _events)
			{
				if (eventTransition.Value.TargetState?.Id == Id)
				{
					throw new InvalidOperationException($"The state {Name} with the event {eventTransition.Key.Name} is pointing to itself on transition");
				}
			}
#endif
		}

		/// <inheritdoc />
		public void OnEnter(Action action)
		{
			_onEnter.Add(action);
		}

		/// <inheritdoc />
		public void OnExit(Action action)
		{
			_onExit.Add(action);
		}

		/// <inheritdoc />
		public ITransition Event(IStatechartEvent statechartEvent)
		{
			var transition = new Transition();

			_events.Add(statechartEvent, transition);

			return transition;
		}

		/// <inheritdoc />
		public ITransition Nest(Action<IStateFactory> setup, bool executeExit = true, bool executeFinal = true)
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} is nesting multiple times");
			}

			_nestStateFactory = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);

			setup(_nestStateFactory);

			_stateFactory.Add(_nestStateFactory.States);

			_executeExit = executeExit;
			_executeFinal = executeFinal;
			_initialInnerState = _nestStateFactory.InitialState;
			_transition = new Transition();

			return _transition;
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out var transition))
			{
				return transition;
			}

			var nextState = _currentInnerState.Trigger(statechartEvent);
			while (nextState != null)
			{
				_currentInnerState = nextState;
				nextState = _currentInnerState.Trigger(null);
			}

			if (_currentInnerState is FinalState)
			{
				return _transition;
			}

			if (_currentInnerState is LeaveState state)
			{
				return state.LeaveTransition;
			}

			return null;
		}
	}
}