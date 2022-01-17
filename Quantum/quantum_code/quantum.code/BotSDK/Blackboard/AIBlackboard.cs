using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class AIBlackboard
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIBlackboardEntry[] Entries;

		[NonSerialized] public Dictionary<String, Int32> Map;

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			Map = new Dictionary<string, Int32>();

			for (Int32 i = 0; i < Entries.Length; i++)
			{
				Map.Add(Entries[i].Key.Key, i);
			}
		}

		// ========== PUBLIC METHODS ==================================================================================

		public Int32 GetEntryID(string key)
		{
			Assert.Check(string.IsNullOrEmpty(key) == false, "The Key cannot be empty or null.");
			Assert.Check(Map.ContainsKey(key) == true, "Key {0} not present in the Blackboard", key);

			return Map[key];
		}

		public bool TryGetEntryID(string key, out Int32 id)
		{
			return Map.TryGetValue(key, out id);
		}

		public string GetEntryName(Int32 id)
		{
			return Entries[id].Key.Key;
		}

		public bool HasEntry(string key)
		{
			for (int i = 0; i < Entries.Length; i++)
			{
				if (Entries[i].Key.Key == key)
				{
					return true;
				}
			}

			return false;
		}

		public AIBlackboardEntry GetEntry(string key)
		{
			for (int i = 0; i < Entries.Length; i++)
			{
				if (Entries[i].Key.Key == key)
				{
					return Entries[i];
				}
			}

			return default;
		}
	}
}
