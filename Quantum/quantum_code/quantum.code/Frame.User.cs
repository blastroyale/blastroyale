using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Extents the <see cref="RNGSession"/>
	/// </summary>
	public static class RngSessionExtension
	{
		public static RNGSession Peek(this ref RNGSession session)
		{
			return session;
		}

		public static bool NextBool(this ref RNGSession session)
		{
			return session.NextInclusive(0, 1) == 0;
		}

		public static FP Sign(this ref RNGSession session)
		{
			return session.NextBool() ? FP.Minus_1 : FP._1;
		}

		public static T RandomElement<T>(this ref RNGSession session, IList<T> list)
		{
			var index = session.Next(0, list.Count);
			return list[index];
		}

		public static FP NextInclusive(this ref RNGSession session, FPVector2 range)
		{
			return session.NextInclusive(range.X, range.Y);
		}
	}

	unsafe partial class Frame
	{
		/// <summary>
		/// The current time since the beginning of the simulation;
		/// </summary>
		public FP Time => Number * DeltaTime;

		public QuantumGameConfig GameConfig =>
			FindAsset<QuantumGameConfigs>(RuntimeConfig.GameConfigs.Id).QuantumConfig;

		public NavMesh NavMesh => FindAsset<NavMesh>(Map.NavMeshLinks[0].Id);
		public QuantumMapConfigs MapConfigs => FindAsset<QuantumMapConfigs>(RuntimeConfig.MapConfigs.Id);
		public QuantumGameModeConfigs GameModeConfigs => FindAsset<QuantumGameModeConfigs>(RuntimeConfig.GameModeConfigs.Id);
		public QuantumWeaponConfigs WeaponConfigs => FindAsset<QuantumWeaponConfigs>(RuntimeConfig.WeaponConfigs.Id);

		public QuantumStatConfigs StatConfigs =>
			FindAsset<QuantumStatConfigs>(RuntimeConfig.StatConfigs.Id);

		public QuantumBaseEquipmentStatConfigs BaseEquipmentStatConfigs =>
			FindAsset<QuantumBaseEquipmentStatConfigs>(RuntimeConfig.BaseEquipmentStatConfigs.Id);

		public QuantumEquipmentStatConfigs EquipmentStatConfigs =>
			FindAsset<QuantumEquipmentStatConfigs>(RuntimeConfig.EquipmentStatConfigs.Id);

		public QuantumEquipmentMaterialStatConfigs EquipmentMaterialStatConfigs =>
			FindAsset<QuantumEquipmentMaterialStatConfigs>(RuntimeConfig.EquipmentMaterialStatConfigs.Id);

		public QuantumConsumableConfigs ConsumableConfigs =>
			FindAsset<QuantumConsumableConfigs>(RuntimeConfig.ConsumableConfigs.Id);

		public QuantumChestConfigs ChestConfigs => FindAsset<QuantumChestConfigs>(RuntimeConfig.ChestConfigs.Id);

		public QuantumSpecialConfigs SpecialConfigs =>
			FindAsset<QuantumSpecialConfigs>(RuntimeConfig.SpecialConfigs.Id);

		public QuantumAssetConfigs AssetConfigs => FindAsset<QuantumAssetConfigs>(RuntimeConfig.AssetConfigs.Id);
		public QuantumBotConfigs BotConfigs => FindAsset<QuantumBotConfigs>(RuntimeConfig.BotConfigs.Id);

		public QuantumBotDifficultyConfigs BotDifficultyConfigs => FindAsset<QuantumBotDifficultyConfigs>(RuntimeConfig.BotDifficultyConfigs.Id);


		public QuantumShrinkingCircleConfigs ShrinkingCircleConfigs =>
			FindAsset<QuantumShrinkingCircleConfigs>(RuntimeConfig.ShrinkingCircleConfigs.Id);

		public QuantumMutatorConfigs MutatorConfigs => FindAsset<QuantumMutatorConfigs>(RuntimeConfig.MutatorConfigs.Id);

		/// <summary>
		/// Requests the list of <typeparamref name="T"/> pointers that can be iterated over.
		/// Use this with caution because creates garbage. A good use is to allow indexing on a execution loop
		/// </summary>
		public List<EntityComponentPointerPair<T>> ComponentPointerList<T>() where T : unmanaged, IComponent
		{
			var list = new List<EntityComponentPointerPair<T>>();

			foreach (var pair in Unsafe.GetComponentBlockIterator<T>())
			{
				list.Add(pair);
			}

			return list;
		}

		/// <summary>
		/// Returns all player datas
		/// </summary>
		/// <returns></returns>
		public IEnumerable<RuntimePlayer> GetAllPlayerDatas()
		{
			return _playerData.Iterator()
				.Select(kp => kp.Value.Player)
				.Where(p => p != null);
		}
		
		public uint GetTeamSize()
		{
			return _runtimeConfig.MatchConfigs.TeamSize;
		}
	}
}