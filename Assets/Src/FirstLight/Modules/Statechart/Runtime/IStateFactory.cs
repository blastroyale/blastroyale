// ReSharper disable CheckNamespace

namespace FirstLight.Statechart
{
	/// <summary>
	/// The state factory is used to setup the <see cref="IStatechart"/> representation of states and transitions
	/// There is always a state factory being created per region in the <see cref="IStatechart"/>.
	/// A state factory is also the data container of the states and transitions of the <see cref="IStatechart"/>
	/// </summary>
	public interface IStateFactory
	{
		/// <inheritdoc cref="IInitialState"/>
		IInitialState Initial(string name);
		/// <inheritdoc cref="IFinalState"/>
		IFinalState Final(string name);
		/// <inheritdoc cref="ISimpleState"/>
		ISimpleState State(string name);
		/// <inheritdoc cref="ITransition"/>
		ITransitionState Transition(string name);
		/// <inheritdoc cref="INestState"/>
		INestState Nest(string name);
		/// <inheritdoc cref="IChoiceState"/>
		IChoiceState Choice(string name);
		/// <inheritdoc cref="IWaitState"/>
		IWaitState Wait(string name);
		/// <inheritdoc cref="ITaskWaitState"/>
		ITaskWaitState TaskWait(string name);
		/// <inheritdoc cref="ISplitState"/>
		ISplitState Split(string name);
		/// <inheritdoc cref="ILeaveState"/>
		ILeaveState Leave(string name);
	}
}