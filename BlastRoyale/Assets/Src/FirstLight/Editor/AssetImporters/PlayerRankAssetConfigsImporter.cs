using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLightEditor.AssetImporter;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	/// <inheritdoc />
	public class PlayerRankAssetConfigsImporter : AssetsConfigsImporter<int, Sprite, PlayerRankAssetConfigs>
	{
		protected override int[] GetIds()
		{
			var path = AddressableId.Configs_GameConfigs.GetConfig().Path;
			var maxRanks = AssetDatabase.LoadAssetAtPath<GameConfigs>(path).Config.MaxPlayerRanks;
			var ids = new int[maxRanks];

			for (var i = 1; i <= maxRanks; i++)
			{
				ids[i - 1] = i;
			}
			
			return ids;
		}

		protected override string IdPattern(int id)
		{
			return $"PlayerRank{id.ToString()}";
		}
	}
}