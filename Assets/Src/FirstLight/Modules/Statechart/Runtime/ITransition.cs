using System;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart
{
	/// <summary>
	/// The transition with the data to make a switch between two <see cref="IState"/>
	/// A transition is triggered based on the input of the state and can have an output if defined
	/// </summary>
	public interface ITransition
	{
		/// <summary>
		/// Adds the the <paramref name="action"/> output to be invoked when the <seealso cref="ITransition"/> is triggered
		/// It will return this transition in order to create chain instructions
		/// </summary>
		ITransition OnTransition(Action action);
		
		/// <summary>
		/// Defines the target <paramref name="state"/> of the transition
		/// </summary>
		void Target(IState state);
	}

	/// <summary>
	/// An extension contract of <see cref="ITransition"/> by having a condition check that will validate
	/// the transition execution to continue.
	/// </summary>
	public interface ITransitionCondition : ITransition
	{
		/// <summary>
		/// Defines a transition <paramref name="condition"/> function to make a check if can execute this transition
		/// If the condition function fails then the transition will not be executed
		/// It will return this transition in order to create chain instructions
		/// </summary>
		ITransition Condition(Func<bool> condition);
	}
}