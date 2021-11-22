using Photon.Deterministic;
using System;
using Quantum.Collections;

namespace Quantum
{
  public unsafe partial struct AIBlackboardComponent
  {
    #region Init/Free
    public void InitializeBlackboardComponent(Frame f, AIBlackboard bbAsset)
    {
      Board = bbAsset;

      var assetEntries = bbAsset.Entries;

      if (Entries.Ptr != default)
      {
        FreeBlackboardComponent(f);
      }

      QList<BlackboardEntry> entriesList = f.AllocateList<BlackboardEntry>(bbAsset.Entries.Length);

      for (int i = 0; i < assetEntries.Length; i++)
      {
        BlackboardValue newValue = CreateValueFromEntry(assetEntries[i]);
        entriesList.Add(new BlackboardEntry { Value = newValue });
        //entriesList.Add(newValue);
      }

      Entries = entriesList;
    }

    public void FreeBlackboardComponent(Frame f)
    {
      if (Entries.Ptr != default)
      {
        f.FreeList(Entries);
        Entries = default;
      }
    }

    private BlackboardValue CreateValueFromEntry(AIBlackboardEntry entry)
    {
      BlackboardValue newValue = new BlackboardValue();

      if (entry.Type == AIBlackboardValueType.Boolean)
      {
        *newValue.BooleanValue = default;
      }

      if (entry.Type == AIBlackboardValueType.Byte)
      {
        *newValue.ByteValue = default;
      }

      if (entry.Type == AIBlackboardValueType.Integer)
      {
        *newValue.IntegerValue = default;
      }

      if (entry.Type == AIBlackboardValueType.FP)
      {
        *newValue.FPValue = default;
      }

      if (entry.Type == AIBlackboardValueType.Vector2)
      {
        *newValue.FPVector2Value = default;
      }

      if (entry.Type == AIBlackboardValueType.Vector3)
      {
        *newValue.FPVector3Value = default;
      }

      if (entry.Type == AIBlackboardValueType.EntityRef)
      {
        *newValue.EntityRefValue = default;
      }

      return newValue;
    }
    #endregion

    #region Getters
    public QBoolean GetBoolean(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.BooleanValue;
    }

    public byte GetByte(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.ByteValue;
    }

    public Int32 GetInteger(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.IntegerValue;
    }

    public FP GetFP(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.FPValue;
    }

    public FPVector2 GetVector2(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.FPVector2Value;
    }

    public FPVector3 GetVector3(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.FPVector3Value;
    }

    public EntityRef GetEntityRef(Frame f, string key)
    {
      var bbValue = GetBlackboardValue(f, key);
      return *bbValue.EntityRefValue;
    }
    #endregion

    #region Setters
    public BlackboardEntry* Set(Frame f, string key, QBoolean value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.BooleanValue = value;

      return valueList.GetPointer(ID);
    }

    public BlackboardEntry* Set(Frame f, string key, byte value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.ByteValue = value;

      return valueList.GetPointer(ID);
    }

    public BlackboardEntry* Set(Frame f, string key, Int32 value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.IntegerValue = value;

      return valueList.GetPointer(ID);
    }

    public BlackboardEntry* Set(Frame f, string key, FP value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.FPValue = value;

      return valueList.GetPointer(ID);
    }

    public BlackboardEntry* Set(Frame f, string key, FPVector2 value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.FPVector2Value = value;

      return valueList.GetPointer(ID);
    }

    public BlackboardEntry* Set(Frame f, string key, FPVector3 value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.FPVector3Value = value;

      return valueList.GetPointer(ID);

    }

    public BlackboardEntry* Set(Frame f, string key, EntityRef value)
    {
      QList<BlackboardEntry> valueList = f.ResolveList(Entries);
      var ID = GetID(f, key);
      *valueList.GetPointer(ID)->Value.EntityRefValue = value;

      return valueList.GetPointer(ID);
    }
    #endregion

    #region Helpers
    public BlackboardEntry* GetBlackboardEntry(Frame f, string key)
    {
      var bbAsset = f.FindAsset<AIBlackboard>(Board.Id);
      var ID = bbAsset.GetEntryID(key);
      var values = f.ResolveList(Entries);
      return values.GetPointer(ID);
    }

    public BlackboardValue GetBlackboardValue(Frame f, string key)
    {
      var bbAsset = f.FindAsset<AIBlackboard>(Board.Id);
      var ID = bbAsset.GetEntryID(key);
      var values = f.ResolveList(Entries);

      return values[ID].Value;
    }

    public Int32 GetID(Frame f, string key)
    {
      var bbAsset = f.FindAsset<AIBlackboard>(Board.Id);
      var ID = bbAsset.GetEntryID(key);

      return ID;
    }

    public bool TryGetID(Frame f, string key, out Int32 id)
    {
      var bbAsset = f.FindAsset<AIBlackboard>(Board.Id);

      return bbAsset.TryGetEntryID(key, out id);
    }

    public bool HasEntry(Frame f, string key)
    {
      var boardAsset = f.FindAsset<AIBlackboard>(Board.Id);
      return boardAsset.HasEntry(key);
    }
    #endregion

    #region BT Specific
    public void RegisterReactiveDecorator(Frame f, string key, BTDecorator decorator)
    {
      var blackboardEntry = GetBlackboardEntry(f, key);

      QList<AssetRefBTDecorator> reactiveDecorators;
      if (blackboardEntry->ReactiveDecorators.Ptr == default)
      {
        reactiveDecorators = f.AllocateList<AssetRefBTDecorator>();
      }
      else
      {
        reactiveDecorators = f.ResolveList<AssetRefBTDecorator>(blackboardEntry->ReactiveDecorators);
      }
      reactiveDecorators.Add(decorator);

      blackboardEntry->ReactiveDecorators = reactiveDecorators;
    }

    public void UnregisterReactiveDecorator(Frame f, string key, BTDecorator decorator)
    {
      var blackboardEntry = GetBlackboardEntry(f, key);

      if (blackboardEntry->ReactiveDecorators.Ptr != default)
      {
        QList<AssetRefBTDecorator> reactiveDecorators = f.ResolveList<AssetRefBTDecorator>(blackboardEntry->ReactiveDecorators);
        reactiveDecorators.Remove(decorator);
        blackboardEntry->ReactiveDecorators = reactiveDecorators;
      }
    }
    #endregion

    #region Debug
    public void Dump(Frame f)
    {
      string dumpText = "";
      var bbAsset = f.FindAsset<AIBlackboard>(Board.Id);
      dumpText += "Blackboard Path and ID: " + bbAsset.Path + "  |  " + Board.Id.Value;

      var valuesList = f.ResolveList(Entries);
      for (int i = 0; i < valuesList.Count; i++)
      {
        string value = "NONE";
        if (valuesList[i].Value.Field == BlackboardValue.BOOLEANVALUE) value = valuesList[i].Value.BooleanValue->Value.ToString();
        if (valuesList[i].Value.Field == BlackboardValue.BYTEVALUE) value = valuesList[i].Value.ByteValue->ToString();
        if (valuesList[i].Value.Field == BlackboardValue.INTEGERVALUE) value = valuesList[i].Value.IntegerValue->ToString();
        if (valuesList[i].Value.Field == BlackboardValue.FPVALUE) value = valuesList[i].Value.FPValue->ToString();
        if (valuesList[i].Value.Field == BlackboardValue.FPVECTOR2VALUE) value = valuesList[i].Value.FPVector2Value->ToString();
        if (valuesList[i].Value.Field == BlackboardValue.FPVECTOR3VALUE) value = valuesList[i].Value.FPVector3Value->ToString();
        if (valuesList[i].Value.Field == BlackboardValue.ENTITYREFVALUE) value = valuesList[i].Value.EntityRefValue->ToString();

        dumpText += "\nName: " + bbAsset.GetEntryName(i) + ", Value: " + value;
      }

      Log.Info(dumpText);
    }
    #endregion
  }
}
