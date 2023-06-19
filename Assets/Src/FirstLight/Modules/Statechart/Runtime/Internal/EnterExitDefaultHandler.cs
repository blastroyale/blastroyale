using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstLight.Statechart.Internal
{
	public class EnterExitDefaultHandler
	{
		private StateInternal _state;

		private readonly IList<Action> _onEnter = new List<Action>();
		private readonly IList<Func<Task>> _onEnterAsync = new List<Func<Task>>();
		private readonly IList<Action> _onExit = new List<Action>();

		internal EnterExitDefaultHandler(StateInternal state)
		{
			_state = state;
		}

		public void Enter()
		{
			for (var i = 0; i < _onEnter.Count; i++)
			{
				_onEnter[i]?.Invoke();
			}

			for (var i = 0; i < _onEnterAsync.Count; i++)
			{
				var func = _onEnterAsync[i];
				func.Invoke();
				
			}
		}

		public void Exit()
		{
			for (var i = 0; i < _onExit.Count; i++)
			{
				_onExit[i]?.Invoke();
			}
		}

		public void OnEnter(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {_state.Name} cannot have a null OnEnter action");
			}

			_onEnter.Add(action);
		}

		public void OnExit(Action action)
		{
			if (action == null)
			{
				throw new NullReferenceException($"The state {_state.Name} cannot have a null OnExit action");
			}

			_onExit.Add(action);
		}

		public void OnEnterAsync(Func<Task> task)
		{
			_onEnterAsync.Add(task);
		}
	}
}