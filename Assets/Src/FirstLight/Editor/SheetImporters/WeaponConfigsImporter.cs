using System.Collections.Generic;
using System.Linq;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEditor;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class WeaponConfigsImporter : GoogleSheetConfigsAssetImporterBase<QuantumWeaponConfig, WeaponConfigs, CustomAssetConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=130698866";

		/// <summary>
		/// Keeps bullet prototype field from maually being set without modifying it
		/// </summary>
		public override void AssignNewConfigs(List<QuantumWeaponConfig> newConfigs, WeaponConfigs scriptableObject)
		{
			var configs = new List<QuantumWeaponConfig>();
			for (var x = 0; x < newConfigs.Count; x++)
			{
				var newCfg = newConfigs[x];
				var old = scriptableObject.Configs.FirstOrDefault(cfg => cfg.Id == newCfg.Id);
				if ((int)old.Id > 0)
				{
					newCfg.BulletPrototype = old.BulletPrototype;
					newCfg.BulletHitPrototype = old.BulletHitPrototype;
					newCfg.BulletEndOfLifetimePrototype = old.BulletEndOfLifetimePrototype;
					newCfg.HitType = old.HitType;
				}
				configs.Add(newCfg);
			}
			scriptableObject.Configs = configs;
		}
		
		protected override QuantumWeaponConfig DeserializeAsset(Dictionary<string, string> data, CustomAssetConfigs assetConfigs)
		{
			var config = QuantumDeserializer.DeserializeTo<QuantumWeaponConfig>(data);
			return config;
		}
	}
}