using System.Collections.Generic;
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
	}
	
	unsafe partial class Frame 
	{
		/// <summary>
		/// The current time since the beginning of the simulation;
		/// </summary>
		public FP Time => Number * DeltaTime;

		internal int TargetAllLayerMask { get; private set; }
		internal QuantumGameConfig GameConfig { get; private set; }
		internal NavMesh NavMesh => FindAsset<NavMesh>(Map.NavMeshLinks[0].Id);
		internal QuantumWeaponConfigs WeaponConfigs => FindAsset<QuantumWeaponConfigs>(RuntimeConfig.WeaponConfigs.Id);
		internal QuantumGearConfigs GearConfigs => FindAsset<QuantumGearConfigs>(RuntimeConfig.GearConfigs.Id);
		internal QuantumConsumableConfigs ConsumableConfigs => FindAsset<QuantumConsumableConfigs>(RuntimeConfig.ConsumableConfigs.Id);
		internal QuantumSpecialConfigs SpecialConfigs => FindAsset<QuantumSpecialConfigs>(RuntimeConfig.SpecialConfigs.Id);
		internal QuantumAssetConfigs AssetConfigs => FindAsset<QuantumAssetConfigs>(RuntimeConfig.AssetConfigs.Id);
		internal QuantumBotConfigs BotConfigs => FindAsset<QuantumBotConfigs>(RuntimeConfig.BotConfigs.Id);
		internal QuantumShrinkingCircleConfigs ShrinkingCircleConfigs => FindAsset<QuantumShrinkingCircleConfigs>(RuntimeConfig.ShrinkingCircleConfigs.Id);

		partial void InitUser()
		{
			GameConfig = FindAsset<QuantumGameConfigs>(RuntimeConfig.GameConfigs.Id).QuantumConfig;
			TargetAllLayerMask = Layers.GetLayerMask("Default", "Playable Target", "Non Playable Target", 
			                                         "Prop", "World", "Environment No Silhouette");
		}
		
		/// <summary>
		/// Requests the list of <typeparamref name="T"/> that can be iterated over.
		/// Use this with caution because creates garbage. A good use is to allow indexing on a execution loop
		/// </summary>
		public List<EntityComponentPair<T>> ComponentList<T>() where T : unmanaged, IComponent
		{
			var list = new List<EntityComponentPair<T>>();

			foreach (var pair in GetComponentIterator<T>())
			{
				list.Add(pair);
			}

			return list;
		}
		
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
	}
}
