using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Server.SDK.Modules;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="ITaskWaitState"/>
	internal class TaskWaitState : StateInternal, ITaskWaitState
	{
		private ITransitionInternal _transition;
		private Func<UniTask> _taskAwaitAction;
		private bool _triggered;
		private bool _completed;
		private uint _executionCount;
		
		private readonly EnterExitDefaultHandler _enterExitHandler;
		private readonly Dictionary<IStatechartEvent, ITransitionInternal> _events = new Dictionary<IStatechartEvent, ITransitionInternal>();

		public TaskWaitState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
			_triggered = false;
			_completed = false;
			_executionCount = 0;
			_enterExitHandler = new EnterExitDefaultHandler(this);

		}

		/// <inheritdoc />
		public override void Enter()
		{
			_triggered = false;
			_completed = false;
			
			_enterExitHandler.Enter();
		}

		/// <inheritdoc />
		public override void Exit()
		{
			_completed = true;
			
			_enterExitHandler.Exit();
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_taskAwaitAction == null)
			{
				throw new MissingMethodException($"The state {Name} doesn't have a task await action");
			}

			if (_transition.TargetState?.Id == Id)
			{
				throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
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
		public ITransition WaitingFor(Func<Task> taskAwaitAction)
		{
			return WaitingFor(() => WrapToUniTask(taskAwaitAction));
		}

		private async UniTask WrapToUniTask(Func<Task> t) => await t();
		
		public ITransition WaitingFor(Func<UniTask> taskAwaitAction)
		{
			_taskAwaitAction = taskAwaitAction ?? throw new NullReferenceException($"The state {Name} cannot have a null wait action");
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
				InnerTaskAwait(statechartEvent?.Name).Forget();
			}

			return _completed ? _transition : null;
		}

		private async UniTaskVoid InnerTaskAwait(string eventName)
		{
			var currentExecution = _executionCount;

			_executionCount++;

			try
			{
				FLog.Verbose("Statechart", $"TaskWait - '{eventName}' : " +
				                           $"'{_taskAwaitAction.Target}.{_taskAwaitAction.Method.Name}()' => '{Name}'");

				await UniTask.Yield();
				await _taskAwaitAction();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the state '{Name}', when calling the task wait action " +
				                    $"'{_taskAwaitAction.Target}.{_taskAwaitAction.Method.Name}()'.\n" + CreationStackTrace, e);
			}

			// Checks if the state didn't exited from an outsource trigger (Nested State) before the Task was completed
			if (!_completed && _executionCount - 1 == currentExecution)
			{
				_completed = true;
				FLog.Verbose("Statechart",Name+"State on move next call "+ModelSerializer.PrettySerialize(_stateFactory.Data.Statechart.CurrentStateDebug()));
				_stateFactory.Data.StateChartMoveNextCall(null);
			}
		}
		public override Dictionary<string, object> CurrentState
		{
			get
			{
				var state = base.CurrentState;
				state.Add("TriggerEvents", _events.ToDictionary(kv => kv.Key.Name, kv => kv.Value.TargetState?.Name));
				state.Add("Completed", _completed);
				state.Add("ExecutionCount", _executionCount);
				state.Add("Triggered", _triggered);
				return state;
			}
		}
		
	}
}