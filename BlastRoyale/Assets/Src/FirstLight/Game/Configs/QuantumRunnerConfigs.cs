using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable object with the necessary config data to start the <see cref="QuantumRunner"/>
	/// </summary>
	[CreateAssetMenu(fileName = "QuantumRunner Configs", menuName = "ScriptableObjects/QuantumRunner Configs")]
	[IgnoreServerSerialization]
	public class QuantumRunnerConfigs : ScriptableObject
	{
		
		[SerializeField] private RuntimeConfig _runtimeConfig;
		[SerializeField] private DeterministicSessionConfigAsset _deterministicConfigAsset;
		[SerializeField] private PhotonServerSettings _serverSettings;

		/// <inheritdoc cref="DeterministicSessionConfigAsset"/>
		public DeterministicSessionConfigAsset DeterministicSessionConfigAsset => _deterministicConfigAsset;

		/// <inheritdoc cref="PhotonServerSettings"/>
		public PhotonServerSettings PhotonServerSettings => _serverSettings;

		/// <summary>
		/// Marks the Quantum simulation to run in offline or online mode
		/// </summary>
		public bool IsOfflineMode { get; set; } = false;

		/// <summary>
		/// Returns the <see cref="RuntimeConfig"/> used to build the simulation from the client side
		/// </summary>
		public RuntimeConfig RuntimeConfig => _runtimeConfig;
	
	}
}