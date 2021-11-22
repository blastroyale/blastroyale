using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="IWaitState"/>
	internal class WaitState : StateInternal, IWaitState
	{
		private ITransitionInternal _transition;
		private IWaitActivityInternal _waitingActivity;
		private Action<IWaitActivity> _waitAction;
		private bool _triggered;
		private uint _executionCount;

		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly Dictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public WaitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_triggered = false;
			_executionCount = 0;
		}

		/// <inheritdoc />
		public override void Enter()
		{
			_waitingActivity.Reset();
			_triggered = false;

			for(int i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			for(int i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_waitingActivity == null)
			{
				throw new MissingMethodException($"The state {Name} doesn't have a waiting activity");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
			}

			foreach (var eventTransition in _events)
			{
				if (eventTransition.Value.TargetState?.Id == Id)
				{
					throw new InvalidOperationException(
						$"The state {Name} with the event {eventTransition.Key.Name} is pointing to itself on transition");
				}
			}
#endif
		}

		/// <inheritdoc />
		public void OnEnter(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null OnEnter action");
			}

			_onEnter.Add(action);
		}

		/// <inheritdoc />
		public void OnExit(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null OnExit action");
			}

			_onExit.Add(action);
		}

		/// <inheritdoc />
		public ITransition Event(IStatechartEvent statechartEvent)
		{
			if (statechartEvent == null)
			{
				throw new NullReferenceException($"The state {Name} cannot have a null event");
			}

			var transition = new Transition();

			_events.Add(statechartEvent, transition);

			return transition;
		}

		/// <inheritdoc />
		public ITransition WaitingFor(Action<IWaitActivity> waitAction)
		{
			_waitAction = waitAction ?? throw new NullReferenceException($"The state {Name} cannot have a null wait action");
			_waitingActivity = new WaitActivity(_stateFactory.Data.StateChartMoveNextCall);
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

			if (!_triggered)
			{
				_triggered = true;
				InnerWait(statechartEvent?.Name);
			}

			return _waitingActivity.IsCompleted && _waitingActivity.ExecutionCount == _executionCount - 1 ? _transition : null;
		}

		private void InnerWait(string eventName)
		{
			_waitingActivity.ExecutionCount = _executionCount;
			_executionCount++;

			try
			{
				if (IsStateLogsEnabled)
				{
					Debug.Log($"'{eventName}' event triggers the wait method '{_waitAction.Method.Name}'" +
					          $"from the object {_waitAction.Target} in the state {Name}");
				}
				_waitAction(_waitingActivity);
			}
			catch(Exception e)
			{
				throw new Exception($"Exception in the state '{Name}', when calling the wait action {_waitAction.Method.Name}" +
				                    $"from the object {_waitAction.Target}.\n" + CreationStackTrace, e);
			}
		}
	}
}
