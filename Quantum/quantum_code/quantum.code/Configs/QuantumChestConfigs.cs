using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Sirenix.OdinInspector;

namespace Quantum
{
	[Serializable]
	public struct WeightedItem
	{
		public SimulationItemConfig ItemConfig;
		public int Weight;
	}
	
	[Serializable]
	public struct SimulationItemConfig
	{
		/// <summary>
		/// Used when item has only a single item id.
		/// Leave "Random" when using metadata
		/// </summary>
		public GameId SimpleGameId;
		
		/// <summary>
		/// Used to define an equipment item
		/// </summary>
		public Equipment EquipmentMetadata;
	}
	
	[Serializable]
	public class WeightedPoolDrop
	{
		/// <summary>
		/// Chance to drop this pool
		/// </summary>
		public FP Chance;
		
		/// <summary>
		/// Amount of items from the pool to drop.
		/// </summary>
		public byte Amount;
		
		/// <summary>
		/// Pool of items that the wheighted random will pick
		/// </summary>
		public List<WeightedItem> Pool;
	}

	[Serializable]
	public partial class QuantumChestConfig
	{
		public GameId Id;
		public ChestType ChestType;
		
		public bool AutoOpen;
		public List<WeightedPoolDrop> DropTables;

		public FP CollectableChestPickupRadius;
		public FP CollectTime;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumChestConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumChestConfigs
	{
		public List<QuantumChestConfig> QuantumConfigs = new List<QuantumChestConfig>();

		private IDictionary<GameId, QuantumChestConfig> _dictionary = null;


		/// <summary>
		/// Checks if this has the given config
		/// </summary>
		public bool HasConfig(GameId id)
		{
			return _dictionary.ContainsKey(id);
		}
		
		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumChestConfig GetConfig(GameId id)
		{
			return _dictionary[id];
		}
		
		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			_dictionary = new Dictionary<GameId, QuantumChestConfig>();
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
			}
		}		

		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="type"/>
		/// </summary>
		public QuantumChestConfig GetConfig(ChestType type)
		{
			return GetConfig(type.GameId());
		}
	}
}
