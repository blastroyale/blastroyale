namespace Quantum
{
	/// <summary>
	/// Quantum asset config container for misc asset refs.
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumAssetConfigs
	{
		public AssetRefEntityPrototype HazardPrototype;
		public AssetRefEntityPrototype PlayerCharacterPrototype;
		public AssetRefEntityPrototype PlayerBulletPrototype;
		public AssetRefNavMeshAgentConfig BotNavMeshConfig;
		public AssetRefEntityPrototype DummyCharacterPrototype;
		public AssetRefEntityPrototype WeaponPlatformPrototype;
		public AssetRefEntityPrototype ConsumablePlatformPrototype;
	}
}