using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="IChoiceState"/>
	internal class ChoiceState : StateInternal, IChoiceState
	{
		private readonly IList<ITransitionInternal> _transitions = new List<ITransitionInternal>();
		private readonly EnterExitDefaultHandler _enterExitHandler;
		
		public ChoiceState(string name, IStateFactoryInternal factory) : base(name, factory)
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