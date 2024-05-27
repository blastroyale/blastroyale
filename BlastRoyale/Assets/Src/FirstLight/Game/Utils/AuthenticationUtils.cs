using System;
using Cinemachine;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helper methods for authentication.
	/// </summary>
	public static class AuthenticationUtils
	{
		
		/// <summary>
		/// This method checks whether a given string, username, is a valid username field.
		/// </summary>
		/// <param name="username"></param>
		/// <returns>true if the username is valid, false if it is not</returns>
		public static bool IsUsernameFieldValid(string username)
		{
			return String.Compare(username, string.Empty, StringComparison.Ordinal) != 0;
		}

		/// <summary>
		/// This method checks whether a given string, password, is a valid password field.
		/// </summary>
		/// <param name="password"></param>
		/// <returns>true if the password is valid, false if it is not</returns>
		public static bool IsPasswordFieldValid(string password)
		{
			return String.Compare(password, string.Empty, StringComparison.Ordinal) != 0;
		}

		/// <summary>
		/// This method checks whether a given string is a valid email address.
		/// </summary>
		/// <param name="email">The email address to be checked.</param>
		/// <returns>true if the email address is valid, false if it is not.</returns>
		public static bool IsEmailFieldValid(string email)
		{
			var trimmedEmail = email.Trim();

			if (trimmedEmail.EndsWith("."))
			{
				return false;
			}

			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == trimmedEmail;
			}
			catch
			{
				return false;
			}
		}
	}
}