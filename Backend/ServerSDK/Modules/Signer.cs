using System.Security.Cryptography;
using System.Text;

namespace ServerSDK.Modules
{
	/// <summary>
	/// Class with functionality to sign messages.
	/// Isolated in server SDK to be shared across services.
	/// </summary>
	public class ServerDataSigner
	{
		/// <summary>
		/// Signs the given message with the given key.
		/// This functionality is to be shared across services so we can validate
		/// any given data across services without remote calls.
		/// </summary>
		public static string Sign(string message, string privateKey)
		{
			using (SHA256 crypt = SHA256.Create())
			{
				return crypt.ComputeHash(
					Encoding.UTF8.GetBytes(message + privateKey))
					.Select(bite => bite.ToString("x2"))
					.Aggregate(new StringBuilder(), (c, n) => c.Append(n)).ToString();
			}
		}
	}
}
