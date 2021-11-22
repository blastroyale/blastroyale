using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc />
	internal interface ITransitionInternal : ITransitionCondition
	{
		/// <summary>
		/// The current target state of this transition
		/// </summary>
		IStateInternal TargetState { get; }
		/// <summary>
		/// True if this transition has a condition to check, false otherwise 
		/// </summary>
		bool HasCondition { get; }
		/// <summary>
		/// Debug stack trace string
		/// </summary>
		string CreationStackTrace { get; }
		
		/// <summary>
		/// Checks the defined transition condition.
		/// Returns true if the condition is met, false otherwise
		/// </summary>
		/// <returns></returns>
		bool CheckCondition();
		/// <summary>
		/// Trigger the defined transition
		/// </summary>
		void TriggerTransition();
	}

	/// <inheritdoc />
	internal class Transition : ITransitionInternal
	{
		private readonly IList<Action> _onTransition = new List<Action>();
		private readonly IList<Func<bool>> _condition = new List<Func<bool>>();

		/// <inheritdoc />
		public IStateInternal TargetState { get; private set; }
		/// <inheritdoc />
		public string CreationStackTrace { get; private set; }
		/// <inheritdoc />
		public bool HasCondition => _condition.Count > 0;

		/// <inheritdoc />
		public bool CheckCondition()
		{
			var ret = true;

			for(var i = 0; i < _condition.Count; i++)
			{
				ret = ret && _condition[i].Invoke();
			}

			return ret;
		}

		/// <inheritdoc />
		public void TriggerTransition()
		{
			for(var i = 0; i < _onTransition.Count; i++)
			{
				_onTransition[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public ITransition Condition(Func<bool> condition)
		{
			if(condition == null)
			{
				throw new NullReferenceException("The transition cannot have a null condition");
			}

#if UNITY_EDITOR || DEBUG
			CreationStackTrace = StatechartUtils.RemoveGarbageFromStackTrace(Environment.StackTrace);
#endif

			_condition.Add(condition);

			return this;
		}

		/// <inheritdoc />
		public ITransition OnTransition(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException("The transition cannot have a null OnTransition action");
			}

#if UNITY_EDITOR || DEBUG
			CreationStackTrace = StatechartUtils.RemoveGarbageFromStackTrace(Environment.StackTrace);
#endif

			_onTransition.Add(action);

			return this;
		}

		/// <inheritdoc />
		public void Target(IState state)
		{
			if (state == null)
			{
				throw new NullReferenceException("The transition cannot have a null target state");
			}

			TargetState = (IStateInternal) state;
		}
	}
}