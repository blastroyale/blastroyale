using System;

namespace Quantum
{
  public enum AIParamSource
  {
    None,
    Value,
    Config,
    Blackboard,
    Func,
  }

  [Serializable]
  public abstract unsafe class AIParam<T>
  {
    public AIParamSource Source = AIParamSource.Value;
    public string Key;
    public T DefaultValue;

    public unsafe T ResolveFromHFSM(Frame frame, EntityRef entity)
    {
      AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
      AIConfig aiConfig = frame.FindAsset<AIConfig>(frame.Unsafe.GetPointer<HFSMAgent>(entity)->Config.Id);

      return Resolve(frame, blackboard, aiConfig);
    }

    public unsafe T ResolveFromGOAP(Frame frame, EntityRef entity)
    {
      AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
      AIConfig aiConfig = frame.FindAsset<AIConfig>(frame.Unsafe.GetPointer<GOAPAgent>(entity)->Config.Id);

      return Resolve(frame, blackboard, aiConfig);
    }

    public unsafe T ResolveFromBT(Frame frame, EntityRef entity)
    {
      AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
      AIConfig aiConfig = frame.FindAsset<AIConfig>(frame.Unsafe.GetPointer<BTAgent>(entity)->Config.Id);

      return Resolve(frame, blackboard, aiConfig);
    }

    /// <summary>
    /// Use this to solve the AIParam value when the source of the value is unkown
    /// </summary>
    public unsafe T Resolve(Frame frame, AIBlackboardComponent* blackboardComponent, AIConfig configData, EntityRef entity = default)
    {
      if (Source == AIParamSource.Value || (Source != AIParamSource.Func && string.IsNullOrEmpty(Key) == true))
        return DefaultValue;

      switch (Source)
      {
        case AIParamSource.Blackboard:
          BlackboardValue blackboardValue = blackboardComponent->GetBlackboardValue(frame, Key);
          return GetBlackboardValue(blackboardValue);

        case AIParamSource.Config:
          AIConfig.KeyValuePair config = configData != null ? configData.Get(Key) : null;
          return config != null ? GetConfigValue(config) : DefaultValue;

        case AIParamSource.Func:
          return GetFuncValue(frame, entity);
      }

      return default(T);
    }

    /// <summary>
    /// Use this if the it is known that the AIParam stores specifically a Blackboard value
    /// </summary>
    public unsafe T ResolveBlackboard(Frame frame, AIBlackboardComponent* blackboardComponent)
    {
      BlackboardValue blackboardValue = blackboardComponent->GetBlackboardValue(frame, Key);
      return GetBlackboardValue(blackboardValue);
    }

    /// <summary>
    /// Use this if the it is known that the AIParam stores specifically a Config value
    /// </summary>
    public unsafe T ResolveConfig(Frame frame, AIConfig configData)
    {
      AIConfig.KeyValuePair config = configData != null ? configData.Get(Key) : null;
      return config != null ? GetConfigValue(config) : DefaultValue;
    }

    /// <summary>
    /// Use this if the it is known that the AIParam stores specifically a Func
    /// </summary>
    public unsafe T ResolveFunc(Frame frame, EntityRef entity)
    {
      return GetFuncValue(frame, entity);
    }

    protected abstract T GetBlackboardValue(BlackboardValue value);
    protected abstract T GetConfigValue(AIConfig.KeyValuePair config);
    protected abstract T GetFuncValue(Frame frame, EntityRef entity);
  }
}
