using System;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// Exception to be used in any part of the logic
	/// </summary>
	[Serializable]
	public class LogicException : Exception
	{
		public LogicException(string message) : base(message)
		{
		}
 
		public LogicException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}