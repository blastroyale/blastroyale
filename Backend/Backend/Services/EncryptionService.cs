using FirstLight.Game.Data;
using ServerSDK.Models;
using ServerSDK.Modules;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


// TODO: Move implementation logic of the signer to the SDK and implement logic
// for siging on the client so server is Game-Agnostic
namespace Backend.Services
{
	/// <summary>
	/// Interface that offers ability to encrypt a given message with a given private key.
	/// </summary>
	public interface IStateSigner
	{
		/// <summary>
		/// Encrypts a given message with a given private key. 
		/// Encryption must be deterministic and collisionless.
		/// </summary>
		string SignState(ServerState serverState, string privateKey);
	}

	/// <summary>
	/// Simple implementation to sign player data for blast royale.
	/// Signs the equipped weapons.
	/// </summary>
	public class BlastRoyaleSigner : IStateSigner
	{
		/// <summary>
		/// Minimal sha256 encryption that includes a private kay to the encrypted data.
		/// </summary>
		public string SignState(ServerState state, string privateKey)
		{
			var playerData = state.DeserializeModel<PlayerData>();
			var equipped = playerData.Equipped.Values.ToList();
			var message = string.Join(",", equipped);
			return ServerDataSigner.Sign(message, privateKey);
		}

	
	}
}
