using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
  public unsafe partial class AIBlackboard
  {
    public AIBlackboardEntry[] Entries;

    [NonSerialized] public Dictionary<String, Int32> Map;

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      Map = new Dictionary<string, Int32>();

      for (Int32 i = 0; i < Entries.Length; i++)
      {
        Map.Add(Entries[i].Key.Key, i);
      }
    }

    public Int32 GetEntryID(string key)
    {
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
