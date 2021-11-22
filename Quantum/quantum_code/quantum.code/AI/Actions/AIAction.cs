namespace Quantum
{
	public abstract unsafe partial class AIAction
	{
		/// <summary>
		/// Will execute this pre update setup before the main <seealso cref="AIAction.Update"/> call.
		/// Important to note that it is only invoked on "Update Actions" and not on "Enter Actions" or "Exit Actions".
		/// Use this for physics query calls.
		/// </summary>
		public virtual void PreUpdate(Frame f, EntityRef e) {}
	}
}