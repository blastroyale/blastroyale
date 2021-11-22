using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="IChoiceState"/>
	internal class ChoiceState : StateInternal, IChoiceState
	{
		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Action> _onExit = new List<Action>();
		private readonly IList<ITransitionInternal> _transitions = new List<ITransitionInternal>();

		public ChoiceState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
		}

		/// <inheritdoc />
		public override void Enter()
		{
			for(var i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Exit()
		{
			for(var i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			var noTransitionConditionCount = 0;

			for(var i = 0; i < _transitions.Count; i++)
			{
				if (!_transitions[i].HasCondition)
				{
					noTransitionConditionCount++;
				}

				if (_transitions[i].TargetState == null)
				{
					throw new MissingMemberException($"The state {Name} transition {i.ToString()} is not pointing to any state");
				}

				if (_transitions[i].TargetState.Id == Id)
				{
					throw new InvalidOperationException($"The state {Name} is pointing to itself on transition");
				}
			}

			if (noTransitionConditionCount == 0)
			{
				throw new MissingMethodException($"Choice state {Name} does not have a transition without a condition");
			}

			if (noTransitionConditionCount > 1)
			{
				UnityEngine.Debug.LogWarning($"Choice state {Name} has multiple transition without a condition defined." +
				                             "This will lead to improper behaviour and will pick a random transition");
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
		public ITransitionCondition Transition()
		{
			var transition = new Transition();

			_transitions.Add(transition);

			return transition;
		}

		/// <inheritdoc />
		protected override ITransitionInternal OnTrigger(IStatechartEvent statechartEvent)
		{
			ITransitionInternal noTransitionCondition = null;

			for(var i = 0; i < _transitions.Count; i++)
			{
				if (!_transitions[i].HasCondition)
				{
					noTransitionCondition = _transitions[i];
					continue;
				}

				if (_transitions[i].CheckCondition())
				{
					return _transitions[i];
				}
			}

			return noTransitionCondition;
		}
	}
}