using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial class QuantumConsumableConfig
	{
		public GameId Id;
		public AssetRefEntityPrototype AssetRef;
		public ConsumableType ConsumableType;
		public QuantumGameModePair<FP> Amount;
		public QuantumGameModePair<FP> ConsumableCollectTime;
		public FP CollectableConsumablePickupRadius;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumConsumableConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumConsumableConfigs
	{
		public List<QuantumConsumableConfig> QuantumConfigs = new List<QuantumConsumableConfig>();

		internal IDictionary<ConsumableType, QuantumConsumableConfig> _byConsumableId = null;
		internal IDictionary<GameId, QuantumConsumableConfig> _byGameId = null; // TODO: Remove

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			var dict = new Dictionary<ConsumableType, QuantumConsumableConfig>();
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				dict[QuantumConfigs[i].ConsumableType] = QuantumConfigs[i];
			}
			_byConsumableId = dict;
			
			var dict2 = new Dictionary<GameId, QuantumConsumableConfig>();
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				dict2.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
			}
			_byGameId = dict2;
		}		
		
		/// <summary>
		/// Requests the <see cref="QuantumConsumableConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumConsumableConfig GetConfig(ConsumableType id)
		{
			_byConsumableId.TryGetValue(id, out var cfg);
			return cfg;
		}
		
		/// <summary>
		/// Requests the <see cref="QuantumConsumableConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumConsumableConfig GetConfig(GameId id)
		{
			_byGameId.TryGetValue(id, out var cfg);
			return cfg;
		}
	}
}