using Photon.Deterministic;
using System;

namespace Quantum
{
  [System.Serializable]
  public unsafe sealed class AIParamInt : AIParam<int> 
  {
    public static implicit operator AIParamInt(int value) { return new AIParamInt() { DefaultValue = value }; }

    public AssetRefAIFunctionInt FunctionRef;

    [NonSerialized] private AIFunctionInt _cachedFunction;

    protected override int GetBlackboardValue(BlackboardValue value)
    {
      return *value.IntegerValue;
    }

    protected override int GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.Integer;
    }

    protected override int GetFuncValue(Frame frame, EntityRef entity)
    {
      if(_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionInt>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamBool : AIParam<bool> 
  {
    public static implicit operator AIParamBool(bool value) { return new AIParamBool() { DefaultValue = value }; }

    public AssetRefAIFunctionBool FunctionRef;

    [NonSerialized] private AIFunctionBool _cachedFunction;

    protected override bool GetBlackboardValue(BlackboardValue value)
    {
      return *value.BooleanValue;
    }

    protected override bool GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.Boolean;
    }

    protected override bool GetFuncValue(Frame frame, EntityRef entity)
    {
      if (_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionBool>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamByte : AIParam<byte> 
  {
    public static implicit operator AIParamByte(byte value) { return new AIParamByte() { DefaultValue = value }; }

    public AssetRefAIFunctionByte FunctionRef;

    [NonSerialized] private AIFunctionByte _cachedFunction;

    protected override byte GetBlackboardValue(BlackboardValue value)
    {
      return *value.ByteValue;
    }

    protected override byte GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.Byte;
    }

    protected override byte GetFuncValue(Frame frame, EntityRef entity)
    {
      if (_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionByte>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamFP : AIParam<FP> 
  {
    public static implicit operator AIParamFP(FP value) { return new AIParamFP() { DefaultValue = value }; }

    public AssetRefAIFunctionFP FunctionRef;

    [NonSerialized] private AIFunctionFP _cachedFunction;

    protected override FP GetBlackboardValue(BlackboardValue value)
    {
      return *value.FPValue;
    }

    protected override FP GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.FP;
    }

    protected override FP GetFuncValue(Frame frame, EntityRef entity)
    {
      if (_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionFP>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamFPVector2 : AIParam<FPVector2> 
  {
    public static implicit operator AIParamFPVector2(FPVector2 value) { return new AIParamFPVector2() { DefaultValue = value }; }

    public AssetRefAIFunctionFPVector2 FunctionRef;

    [NonSerialized] private AIFunctionFPVector2 _cachedFunction;

    protected override FPVector2 GetBlackboardValue(BlackboardValue value)
    {
      return *value.FPVector2Value;
    }

    protected override FPVector2 GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.FPVector2;
    }

    protected override FPVector2 GetFuncValue(Frame frame, EntityRef entity)
    {
      if (_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionFPVector2>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamFPVector3 : AIParam<FPVector3> 
  {
    public static implicit operator AIParamFPVector3(FPVector3 value) { return new AIParamFPVector3() { DefaultValue = value }; }

    public AssetRefAIFunctionFPVector3 FunctionRef;

    [NonSerialized] private AIFunctionFPVector3 _cachedFunction;

    protected override FPVector3 GetBlackboardValue(BlackboardValue value)
    {
      return *value.FPVector3Value;
    }

    protected override FPVector3 GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.FPVector3;
    }

    protected override FPVector3 GetFuncValue(Frame frame, EntityRef entity)
    {
      if (_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionFPVector3>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamString : AIParam<string> 
  {
    public static implicit operator AIParamString(string value) { return new AIParamString() { DefaultValue = value }; }

    protected override string GetBlackboardValue(BlackboardValue value)
    {
      throw new NotSupportedException("Blackboard variables as strings are not supported.");
    }

    protected override string GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.String;
    }

    protected override string GetFuncValue(Frame frame, EntityRef entity)
    {
      return default;
    }
  }

  [System.Serializable]
  public unsafe sealed class AIParamEntityRef : AIParam<EntityRef> 
  {
    public static implicit operator AIParamEntityRef(EntityRef value) { return new AIParamEntityRef() { DefaultValue = value }; }

    public AssetRefAIFunctionEntityRef FunctionRef;

    [NonSerialized] private AIFunctionEntityRef _cachedFunction;

    protected override EntityRef GetBlackboardValue(BlackboardValue value)
    {
      return *value.EntityRefValue;
    }

    protected override EntityRef GetConfigValue(AIConfig.KeyValuePair config)
    {
      return config.Value.EntityRef;
    }

    protected override EntityRef GetFuncValue(Frame frame, EntityRef entity)
    {
      if (_cachedFunction == null)
      {
        _cachedFunction = frame.FindAsset<AIFunctionEntityRef>(FunctionRef.Id);
      }

      return _cachedFunction.Execute(frame, entity);
    }
  }
}
