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

		internal QuantumGameConfig GameConfig =>
			FindAsset<QuantumGameConfigs>(RuntimeConfig.GameConfigs.Id).QuantumConfig;

		internal NavMesh NavMesh => FindAsset<NavMesh>(Map.NavMeshLinks[0].Id);
		internal QuantumMapConfigs MapConfigs => FindAsset<QuantumMapConfigs>(RuntimeConfig.MapConfigs.Id);
		internal QuantumGameModeConfigs GameModeConfigs => FindAsset<QuantumGameModeConfigs>(RuntimeConfig.GameModeConfigs.Id);
		internal QuantumWeaponConfigs WeaponConfigs => FindAsset<QuantumWeaponConfigs>(RuntimeConfig.WeaponConfigs.Id);

		internal QuantumStatConfigs StatConfigs =>
			FindAsset<QuantumStatConfigs>(RuntimeConfig.StatConfigs.Id);

		internal QuantumBaseEquipmentStatConfigs BaseEquipmentStatConfigs =>
			FindAsset<QuantumBaseEquipmentStatConfigs>(RuntimeConfig.BaseEquipmentStatConfigs.Id);

		internal QuantumEquipmentStatConfigs EquipmentStatConfigs =>
			FindAsset<QuantumEquipmentStatConfigs>(RuntimeConfig.EquipmentStatConfigs.Id);

		internal QuantumEquipmentMaterialStatConfigs EquipmentMaterialStatConfigs =>
			FindAsset<QuantumEquipmentMaterialStatConfigs>(RuntimeConfig.EquipmentMaterialStatConfigs.Id);

		internal QuantumConsumableConfigs ConsumableConfigs =>
			FindAsset<QuantumConsumableConfigs>(RuntimeConfig.ConsumableConfigs.Id);

		internal QuantumChestConfigs ChestConfigs => FindAsset<QuantumChestConfigs>(RuntimeConfig.ChestConfigs.Id);

		internal QuantumSpecialConfigs SpecialConfigs =>
			FindAsset<QuantumSpecialConfigs>(RuntimeConfig.SpecialConfigs.Id);

		internal QuantumAssetConfigs AssetConfigs => FindAsset<QuantumAssetConfigs>(RuntimeConfig.AssetConfigs.Id);
		internal QuantumBotConfigs BotConfigs => FindAsset<QuantumBotConfigs>(RuntimeConfig.BotConfigs.Id);

		internal QuantumBotDifficultyConfigs BotDifficultyConfigs => FindAsset<QuantumBotDifficultyConfigs>(RuntimeConfig.BotDifficultyConfigs.Id);


		internal QuantumShrinkingCircleConfigs ShrinkingCircleConfigs =>
			FindAsset<QuantumShrinkingCircleConfigs>(RuntimeConfig.ShrinkingCircleConfigs.Id);

		internal QuantumMutatorConfigs MutatorConfigs => FindAsset<QuantumMutatorConfigs>(RuntimeConfig.MutatorConfigs.Id);

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
		
		public int GetTeamSize()
		{
			return _runtimeConfig.TeamSize;
		}
	}
}