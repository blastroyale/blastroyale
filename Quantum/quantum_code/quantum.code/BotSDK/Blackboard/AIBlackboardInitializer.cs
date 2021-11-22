using Photon.Deterministic;
using System;

namespace Quantum
{
  public unsafe partial class AIBlackboardInitializer
  {
    [Serializable]
    public struct AIBlackboardInitialValue
    {
      public Boolean AsBoolean;
      public Byte AsByte;
      public Int32 AsInteger;
      public FP AsFP;
      public FPVector2 AsFPVector2;
      public FPVector3 AsFPVector3;
      public EntityRef AsEntityRef;
    }

    [Serializable]
    public struct AIBlackboardInitialValueEntry
    {
      public string Key;
      public AIBlackboardInitialValue Value;
    }

    public bool ReportMissingEntries = true;

    public AssetRefAIBlackboard AIBlackboard;
    public AIBlackboardInitialValueEntry[] InitialValues;


    public unsafe static void InitializeBlackboard(Frame f, AIBlackboardComponent* bb, AIBlackboardInitializer bbInitialState, AIBlackboardInitialValueEntry[] bbOverrides = null)
    {
      AIBlackboard board = f.FindAsset<AIBlackboard>(bbInitialState.AIBlackboard.Id);

      bb->InitializeBlackboardComponent(f, board);

      ApplyEntries(f, bb, bbInitialState, bbInitialState.InitialValues);
      ApplyEntries(f, bb, bbInitialState, bbOverrides);
    }

    public unsafe static void ApplyEntries(Frame f, AIBlackboardComponent* bb, AIBlackboardInitializer bbInitialState, AIBlackboardInitialValueEntry[] values)
    {
      if (values == null) return;

      for (int i = 0; i < values.Length; i++)
      {
        string key = values[i].Key;
        if (bb->HasEntry(f, key) == false)
        {
          if (bbInitialState.ReportMissingEntries)
          {
            Quantum.Log.Warn($"Blackboard {bb->Board} does not have an entry with a key called '{key}'");
          }
          continue;
        }

        BlackboardValue value = bb->GetBlackboardValue(f, key);
        switch (value.Field)
        {
          case BlackboardValue.BOOLEANVALUE:
            bb->Set(f, key, values[i].Value.AsBoolean);
            break;
          case BlackboardValue.BYTEVALUE:
            bb->Set(f, key, values[i].Value.AsByte);
            break;
          case BlackboardValue.ENTITYREFVALUE:
            bb->Set(f, key, values[i].Value.AsEntityRef);
            break;
          case BlackboardValue.FPVALUE:
            bb->Set(f, key, values[i].Value.AsFP);
            break;
          case BlackboardValue.INTEGERVALUE:
            bb->Set(f, key, values[i].Value.AsInteger);
            break;
          case BlackboardValue.FPVECTOR2VALUE:
            bb->Set(f, key, values[i].Value.AsFPVector2);
            break;
          case BlackboardValue.FPVECTOR3VALUE:
            bb->Set(f, key, values[i].Value.AsFPVector3);
            break;
        }
      }
    }
  }
}
