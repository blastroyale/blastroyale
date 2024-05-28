// ReSharper disable CheckNamespace

namespace FirstLight.Statechart
{
	/// <summary>
	/// The controller for the <see cref="IWaitState"/>
	/// This activity has the behaviour to complete the execution of the <see cref="IWaitState"/> and can also create
	/// more layers of activities to create a bigger split of control over the <see cref="IWaitState"/> that is controlling
	/// </summary>
	public interface IWaitActivity
	{
		/// <summary>
		/// Marks the <see cref="IWaitState"/> as completed to continue the state's execution process.
		/// Returns true if all it's inner activities are also completed, false otherwise.
		/// </summary>
		bool Complete();
		
		/// <summary>
		/// Creates and returns a new inner activity to have a new layer to hold the <see cref="IState"/>.
		/// The new inner activity create will be a child of this one and after the completion of this activity
		/// is only successful when also all it's children also complete
		/// </summary>
		IWaitActivity Split();
	}
}