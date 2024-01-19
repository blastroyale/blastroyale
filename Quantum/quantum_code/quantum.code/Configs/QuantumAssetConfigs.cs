namespace Quantum
{
	/// <summary>
	/// Quantum asset config container for misc asset refs.
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumAssetConfigs
	{
		public AssetRefEntityPrototype PlayerCharacterPrototype;
		public AssetRefEntityPrototype DefaultBulletPrototype;
		public AssetRefEntityPrototype DummyCharacterPrototype;
		public AssetRefEntityPrototype WeaponPlatformPrototype;
		public AssetRefEntityPrototype ConsumablePlatformPrototype;
		public AssetRefEntityPrototype EquipmentPickUpPrototype;
		public AssetRefEntityPrototype ChestPrototype;
		public AssetRefEntityPrototype AirDropPrototype;
		public AssetRefEntityPrototype LandMinePrototype;
		public AssetRefNavMeshAgentConfig BotNavMeshConfig;
		public AssetRefCharacterController3DConfig BotKccConfig;
	}
}