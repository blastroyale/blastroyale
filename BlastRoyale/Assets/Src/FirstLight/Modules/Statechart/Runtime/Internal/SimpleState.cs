using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="ISimpleState"/>
	internal class SimpleState : StateInternal, ISimpleState
	{
		private readonly EnterExitDefaultHandler _enterExitHandler;
		private readonly IDictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public SimpleState(string name, IStateFactoryInternal factory) : base(name, factory)
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
			foreach(var eventTransition in _events)
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
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			if (statechartEvent != null && _events.TryGetValue(statechartEvent, out var transition))
			{
				return transition;
			}

			return null;
		}

		public override Dictionary<string, object> CurrentState
		{
			get
			{
				var state = base.CurrentState;
				state.Add("TriggerEvents", _events.ToDictionary(kv => kv.Key.Name, kv => kv.Value.TargetState.Name));
				return state;
			}
		}
	}
}