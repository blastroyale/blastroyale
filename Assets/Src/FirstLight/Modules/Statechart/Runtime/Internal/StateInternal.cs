using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FirstLight.FLogger;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="IState"/>
	internal interface IStateInternal : IState, IEquatable<IStateInternal>
	{
		/// <summary>
		/// The unique value identifying this state
		/// </summary>
		uint Id { get; }

		/// <summary>
		/// The string representation identifying this state
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The layer in the nested setup this state is in. If in the root then the value will be 0
		/// </summary>
		uint RegionLayer { get; }

		/// <summary>
		/// The stack trace when this setup was created. Relevant for debugging purposes
		/// </summary>
		string CreationStackTrace { get; }

		string Creator { get; }

		/// <summary>
		/// Triggers the given <paramref name="statechartEvent"/> as input to the <see cref="IStatechart"/> and returns
		/// the processed <see cref="IStateInternal"/> as an output
		/// </summary>
		IStateInternal Trigger(IStatechartEvent statechartEvent);

		/// <summary>
		/// Marks the initial moment of this state as the new current state in the <see cref="IStatechart"/>
		/// </summary>
		void Enter();

		/// <summary>
		/// Marks the final moment of this state as the current state in the <see cref="IStatechart"/>
		/// </summary>
		void Exit();

		/// <summary>
		/// Validates this state to any potential bad setup schemes. Relevant to debug purposes.
		/// It requires the <see cref="IStatechart"/> to run at runtime.
		/// </summary>
		void Validate();

		public Dictionary<string, object> CurrentState { get; }
	}

	/// <inheritdoc />
	internal abstract class StateInternal : IStateInternal, IStateDebug
	{
		protected readonly IStateFactoryInternal _stateFactory;

		private static uint _idRef;

		/// <inheritdoc />
		public uint Id { get; }

		/// <inheritdoc />
		public string Name { get; }

		/// <inheritdoc />
		public uint RegionLayer => _stateFactory.RegionLayer;

		/// <inheritdoc />
		public string CreationStackTrace { get; }

		public string Creator { get; private set; }

		public bool RunningAsync { get; private set; }

		public virtual Dictionary<string, object> CurrentState => new ()
		{
			{"Name", Name},
			{"Creator", Creator},
			{"Type", GetType().Name}
		};

		protected StateInternal(string name, IStateFactoryInternal stateFactory)
		{
			Id = ++_idRef;
			Name = name;

			_stateFactory = stateFactory;

#if UNITY_EDITOR || DEBUG
			CreationStackTrace = StatechartUtils.RemoveGarbageFromStackTrace(Environment.StackTrace);
			var stack = new StackTrace(true);
			foreach (var stackFrame in stack.GetFrames())
			{
				var fileName = stackFrame.GetFileName();
				if (fileName == null) continue;
				if (!fileName.Contains("FirstLight" + Path.DirectorySeparatorChar + "Game")) continue;
				Creator = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar)+1).Replace(".cs", "");
				break;
			}
#endif
		}

		[Conditional("DEBUG")]
		private void LogStateLoop(IStatechartEvent statechartEvent)
		{
			FLog.Verbose("Statechart", $"{Creator} - '{statechartEvent?.Name}' : " +
				$"'{Name}' -> '{Name}' because => {GetType().UnderlyingSystemType.Name}");
		}

		[Conditional("DEBUG")]
		private void LogExit()
		{
			FLog.Verbose("Statechart", $"{Creator} Exiting '{Name}'");
		}

		[Conditional("DEBUG")]
		private void LogTransition(string eventName, ITransitionInternal transition)
		{
			if (eventName == null)
			{
				FLog.Verbose("Statechart", $"{Creator} transition complete " +
					$"{Name} -> {transition.TargetState?.Name ?? "empty OnTransition()"}'");
			}
			else
			{
				FLog.Verbose("Statechart", $"{Creator} received '{eventName}' causing " +
					$"{Name} -> {transition.TargetState?.Name ?? "only invokes OnTransition()"}'");
			}
		}

		private void LogEnter(IStateInternal state)
		{
			FLog.Verbose("Statechart", $"{state.Creator} Entering '{state.Name}'");
		}

		/// <inheritdoc />
		public IStateInternal Trigger(IStatechartEvent statechartEvent)
		{
			var transition = OnTrigger(statechartEvent);

			if (transition == null)
			{
				return null;
			}

			var nextState = transition.TargetState;

			if (Equals(nextState))
			{
				LogStateLoop(statechartEvent);

				return nextState;
			}

			if (nextState == null)
			{
				TriggerTransition(transition, statechartEvent?.Name);

				return null;
			}

			TriggerExit();
			TriggerTransition(transition, statechartEvent?.Name);
			TriggerEnter(nextState);

			return nextState;
		}

		/// <inheritdoc />
		public bool Equals(IStateInternal stateInternal)
		{
			return stateInternal != null && Id == stateInternal.Id;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is IStateInternal stateBase && Equals(stateBase);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return (int) Id;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Name;
		}

		public virtual string CurrentStateDebug()
		{
			return $"{Name} <i>{GetType().Name}</i>";
		}

		/// <inheritdoc />
		public abstract void Enter();

		/// <inheritdoc />
		public abstract void Exit();

		/// <inheritdoc />
		public abstract void Validate();

		protected abstract ITransitionInternal OnTrigger(IStatechartEvent statechartEvent);

		private void TriggerEnter(IStateInternal state)
		{
			try
			{
				LogEnter(state);

				state.Enter();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the state {state.Name}, OnEnter() actions.\n" + CreationStackTrace,
					e);
			}
		}

		private void TriggerExit()
		{
			try
			{
				LogExit();
				Exit();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the state '{Name}', OnExit() actions.\n" + CreationStackTrace, e);
			}
		}

		private void TriggerTransition(ITransitionInternal transition, string eventName)
		{
			try
			{
				LogTransition(eventName, transition);

				transition.TriggerTransition();
			}
			catch (Exception e)
			{
				throw new Exception($"Exception in the transition '{Name}' -> '{transition?.TargetState?.Name}'," +
					$" TriggerTransition() actions.\n{transition?.CreationStackTrace}", e);
			}
		}
	}
}