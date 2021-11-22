using System;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This struct data encapsulates an action delegate into a struct value type
	/// </summary>
	public struct ActionStruct
	{
		private readonly Action _action;

		public ActionStruct(Action action)
		{
			_action = action;
		}

		/// <summary>
		/// Executes the action
		/// </summary>
		public void Execute()
		{
			_action();
		}

		public static implicit operator ActionStruct(Action action)
		{
			return new ActionStruct(action);
		}
	}
}