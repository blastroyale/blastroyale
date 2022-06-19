using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
	/// <summary>
	/// Interface that offers ability to encrypt a given message with a given private key.
	/// </summary>
	public interface IEncryptionService
	{
		/// <summary>
		/// Encrypts a given message with a given private key. 
		/// Encryption must be deterministic and collisionless.
		/// </summary>
		string Encrypt(string message, string privateKey);
	}

	/// <summary>
	/// Standard RSA implementation using private/public keys.
	/// </summary>
	public class SimpleSha1Encryption : IEncryptionService
	{
		/// <summary>
		/// Minimal sha256 encryption that includes a private kay to the encrypted data.
		/// </summary>
		public string Encrypt(string message, string privateKey)
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
