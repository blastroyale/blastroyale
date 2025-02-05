using System;

namespace FirstLight.Game.Services.Authentication
{
	public class AuthenticationException : Exception
	{
		public bool Recoverable;

		public AuthenticationException(string message, bool recoverable) : base(message)
		{
			Recoverable = recoverable;
		}
	}
}