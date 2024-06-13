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
		
		private object _lock = new object();


		/// <summary>
		/// Requests the <see cref="QuantumConsumableConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumConsumableConfig GetConfig(ConsumableType id)
		{
			if (_byConsumableId == null)
			{
				lock (_lock)
				{
					var dict = new Dictionary<ConsumableType, QuantumConsumableConfig>();

					for (var i = 0; i < QuantumConfigs.Count; i++)
					{
						dict[QuantumConfigs[i].ConsumableType] = QuantumConfigs[i];
					}
					_byConsumableId = dict;
				}
			}
			_byConsumableId.TryGetValue(id, out var cfg);
			return cfg;
		}
		
		/// <summary>
		/// Requests the <see cref="QuantumConsumableConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumConsumableConfig GetConfig(GameId id)
		{
			if (_byConsumableId == null)
			{
				lock (_lock)
				{
					var dict = new Dictionary<GameId, QuantumConsumableConfig>();

					for (var i = 0; i < QuantumConfigs.Count; i++)
					{
						dict.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
					}

					_byGameId = dict;
				}
			}
			_byGameId.TryGetValue(id, out var cfg);
			return cfg;
		}
	}
}