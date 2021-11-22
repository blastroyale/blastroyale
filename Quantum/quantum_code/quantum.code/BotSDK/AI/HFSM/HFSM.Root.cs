using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
  public partial class HFSMRoot : AssetObject
  {
    public string Label;

    public AssetRefHFSMState[] StatesLinks;

    public AssetRefHFSMState InitialState
    {
      get
      {
		    if(StatesLinks != null)
        {
          return StatesLinks[0];
        }
        return default;
      }
    }

    public string[] EventsNames;

    [NonSerialized]
    public Dictionary<string, int> RegisteredEvents = new Dictionary<string, int>();

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      RegisteredEvents.Clear();
      for (int i = 0; i < EventsNames.Length; i++)
      {
        RegisteredEvents.Add(EventsNames[i], i + 1);
      }
    }

    public string GetEventName(int eventID)
    {
      foreach (var kvp in RegisteredEvents)
      {
        if (kvp.Value == eventID)
          return kvp.Key;
      }
      return "";
    }
  }
}