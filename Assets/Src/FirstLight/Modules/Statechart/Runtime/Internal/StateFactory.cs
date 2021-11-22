using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc />
	internal interface IStateFactoryInternal : IStateFactory
	{
		/// <summary>
		/// The Layer that this <see cref="IStateFactory"/> is building upon
		/// </summary>
		uint RegionLayer { get; }
		/// <summary>
		/// The current <see cref="InitialState"/> that this <see cref="IStateFactory"/> is building upon
		/// </summary>
		InitialState InitialState { get; }
		/// <summary>
		/// The current <see cref="FinalState"/> that this <see cref="IStateFactory"/> is building upon
		/// </summary>
		FinalState FinalState { get; }
		/// <summary>
		/// The current list of states that this <see cref="IStateFactory"/> is building upon
		/// </summary>
		IList<IStateInternal> States { get; }
		/// <summary>
		/// The <see cref="StateFactoryData"/> that this <see cref="IStateFactory"/> is building upon
		/// </summary>
		StateFactoryData Data { get; }
		
		/// <summary>
		/// Adds the given list of <paramref name="states"/> to this <see cref="IStateFactory"/> to building upon
		/// </summary>
		/// <param name="states"></param>
		void Add(IList<IStateInternal> states);
	}

	/// <inheritdoc />
	internal class StateFactory : IStateFactoryInternal
	{
		/// <inheritdoc />
		public uint RegionLayer { get; private set; }
		/// <inheritdoc />
		public InitialState InitialState { get; private set; }
		/// <inheritdoc />
		public FinalState FinalState { get; private set; }
		/// <inheritdoc />
		public IList<IStateInternal> States { get; private set; }
		/// <inheritdoc />
		public StateFactoryData Data { get; private set; }

		private StateFactory()
		{
		}

		public StateFactory(uint layer, StateFactoryData data)
		{
			States = new List<IStateInternal>();

			Data = data;
			RegionLayer = layer;
		}

		/// <inheritdoc />
		public void Add(IList<IStateInternal> states)
		{
			for (var i = 0; i < states.Count; i++)
			{
				States.Add(states[i]);
			}
		}

		/// <inheritdoc />
		public IInitialState Initial(string name)
		{
			if (InitialState != null)
			{
				throw new InvalidOperationException($"Initial state already set to {InitialState.Name}");
			}

			var initial = new InitialState(name, this);

			InitialState = initial;

			States.Add(initial);

			return initial;
		}

		/// <inheritdoc />
		public IFinalState Final(string name)
		{
			if (FinalState != null)
			{
				throw new InvalidOperationException($"Final state already set to {FinalState.Name}");
			}

			var state = new FinalState(name, this);

			FinalState = state;

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ISimpleState State(string name)
		{
			var state = new SimpleState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ITransitionState Transition(string name)
		{
			var state = new TransitionState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public INestState Nest(string name)
		{
			var state = new NestState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public IWaitState Wait(string name)
		{
			var state = new WaitState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ITaskWaitState TaskWait(string name)
		{
			var state = new TaskWaitState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public IChoiceState Choice(string name)
		{
			var state = new ChoiceState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ISplitState Split(string name)
		{
			var state = new SplitState(name, this);

			States.Add(state);

			return state;
		}

		/// <inheritdoc />
		public ILeaveState Leave(string name)
		{
			var state = new LeaveState(name, this);

			States.Add(state);

			return state;
		}
	}
}