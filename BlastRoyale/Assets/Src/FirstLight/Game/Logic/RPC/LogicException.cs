using System;

namespace FirstLight.Game.Logic.RPC
{
	/// <summary>
	/// Exception to be used in any part of the logic
	/// </summary>
	[Serializable]
	public class LogicException : Exception
	{
		public int ErrorCode;

		public LogicException(string message, int errorCode) : base(message)
		{
			ErrorCode = errorCode;
		}

		public LogicException(string message) : base(message)
		{
		}

		public LogicException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}