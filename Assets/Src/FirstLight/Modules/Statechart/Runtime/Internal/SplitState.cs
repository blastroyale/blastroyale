using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="ISplitState"/>
	internal class SplitState : StateInternal, ISplitState
	{
		private readonly EnterExitDefaultHandler _enterExitHandler;
		protected readonly IDictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();
		protected readonly IList<InnerStateData> _innerStates = new List<InnerStateData>();


		private ITransitionInternal _transition;

		public SplitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_enterExitHandler = new EnterExitDefaultHandler(this);
		}

		/// <inheritdoc />
		public override void Enter()
		{
			for (var i = 0; i < _innerStates.Count; i++)
			{
				var innerState = _innerStates[i];

				innerState.CurrenState = innerState.InitialState;

				_innerStates[i] = innerState;
			}

			_enterExitHandler.Enter();
		}

		/// <inheritdoc />
		public override void Exit()
		{
			for (var i = 0; i < _innerStates.Count; i++)
			{
				var innerState = _innerStates[i];

				if (innerState.ExecuteExit)
				{
					innerState.CurrenState.Exit();
				}

				if (innerState.ExecuteFinal && !(innerState.CurrenState is FinalState) && !(innerState.CurrenState is LeaveState))
				{
					innerState.NestedFactory.FinalState?.Enter();
				}
			}

			_enterExitHandler.Exit();
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_innerStates.Count < 2)
			{
				throw new MissingMemberException($"Split state {Name} doesn't have enough nested setup defined." +
					$"It needs min 2 nested states to be a proper {nameof(ISplitState)}");
			}
#endif
			OnValidate();
		}

		protected void OnValidate()
		{
#if UNITY_EDITOR || DEBUG
			if (_innerStates.Count == 0)
			{
				throw new MissingMemberException($"This state {Name} doesn't have the nested setup defined correctly");
			}

			for (var i = 0; i < _innerStates.Count; i++)
			{
				var innerState = _innerStates[i];

				if (innerState.ExecuteExit && innerState.NestedFactory == null)
				{
					throw new MissingMemberException($"This state {Name} doesn't have a final state in his first nested " +
						$"setup and is marked to execute it's {nameof(IFinalState.OnEnter)} when completed");
				}
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
		public ITransition Split(params Action<IStateFactory>[] data)
		{
			var array = new NestedStateData[data.Length];

			for (var i = 0; i < array.Length; i++)
			{
				array[i] = data[i];
			}

			return Split(array);
		}

		/// <inheritdoc />
		public ITransition Split(params NestedStateData[] data)
		{
			if (_transition != null)
			{
				throw new InvalidOperationException($"State {Name} is nesting multiple times");
			}

			foreach (var stateData in data)
			{
				var factory = new StateFactory(_stateFactory.RegionLayer + 1, _stateFactory.Data);

				stateData.Setup(factory);

				_stateFactory.Add(factory.States);
				_innerStates.Add(new InnerStateData
				{
					InitialState = factory.InitialState,
					CurrenState = null,
					NestedFactory = factory,
					ExecuteExit = stateData.ExecuteExit,
					ExecuteFinal = stateData.ExecuteFinal
				});
			}

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

			for (var i = 0; i < _innerStates.Count; i++)
			{
				var innerState = _innerStates[i];
				var nextState = innerState.CurrenState.Trigger(statechartEvent);

				while (nextState != null)
				{
					innerState.CurrenState = nextState;
					nextState = innerState.CurrenState.Trigger(null);
				}

				_innerStates[i] = innerState;
			}

			var areAllFinished = true;
			for (var i = 0; i < _innerStates.Count; i++)
			{
				var currenState = _innerStates[i].CurrenState;

				if (currenState is LeaveState leaveState)
				{
					return leaveState.LeaveTransition;
				}

				if (currenState is not FinalState)
				{
					areAllFinished = false;
				}
			}

			return areAllFinished ? _transition : null;
		}

		public override string CurrentStateDebug()
		{
			var str = new StringBuilder();
			str.AppendLine($"{Name} <b>{GetType().Name}</b>");
			foreach (var innerStateData in _innerStates)
			{
				if (innerStateData.CurrenState is IStateDebug debug)
				{
					str.AppendLine("\u21b3 " + string.Join("\n", debug.CurrentStateDebug().Split("\n")
						.Select(a => "  " + a)));
				}
			}

			return str.ToString();
		}

		public override Dictionary<string, object> CurrentState
		{
			get
			{
				var state = base.CurrentState;
				state.Add("InnerStates", _innerStates.Select(s => s.CurrenState.CurrentState).ToList());
				state.Add("TriggerEvents", _events.ToDictionary(kv => kv.Key.Name, kv => kv.Value.TargetState.Name));
				return state;
			}
		}
	}
}