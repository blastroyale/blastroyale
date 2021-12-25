using System;

namespace Quantum
{
	public enum AIParamSource
	{
		None,
		Value,
		Config,
		Blackboard,
		Function,
	}

	// ============================================================================================================

	[Serializable]
	public abstract unsafe class AIParam<T>
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AIParamSource Source = AIParamSource.Value;
		public string Key;
		public T DefaultValue;

		// ========== AIParam INTERFACE ================================================================================

		protected abstract T GetBlackboardValue(BlackboardValue value);
		protected abstract T GetConfigValue(AIConfig.KeyValuePair configPair);

		protected abstract T GetFunctionValue(Frame frame, EntityRef entity);
		protected abstract T GetFunctionValue(FrameThreadSafe frame, EntityRef entity);

		// ========== PUBLIC METHODS ==================================================================================

		public T Resolve(Frame frame, EntityRef entity, AIBlackboardComponent* blackboard, AIConfig aiConfig)
		{
			return Resolve((FrameThreadSafe)frame, entity, blackboard, aiConfig);
		}

		/// <summary>
		/// Use this to solve the AIParam value when the source of the value is unkown
		/// </summary>
		public T Resolve(FrameThreadSafe frame, EntityRef entity, AIBlackboardComponent* blackboard, AIConfig aiConfig)
		{
			if (Source == AIParamSource.Value || (Source != AIParamSource.Function && string.IsNullOrEmpty(Key) == true))
				return DefaultValue;

			switch (Source)
			{
				case AIParamSource.Blackboard:
					BlackboardValue blackboardValue = blackboard->GetBlackboardValue(frame, Key);
					return GetBlackboardValue(blackboardValue);

				case AIParamSource.Config:
					AIConfig.KeyValuePair configPair = aiConfig != null ? aiConfig.Get(Key) : null;
					return configPair != null ? GetConfigValue(configPair) : DefaultValue;

				case AIParamSource.Function:
					return GetFunctionValue(frame, entity);
			}

			return default(T);
		}

		/// <summary>
		/// Use this if the it is known that the AIParam stores specifically a Blackboard value
		/// </summary>
		public unsafe T ResolveBlackboard(Frame frame, AIBlackboardComponent* blackboard)
		{
			return ResolveBlackboard((FrameThreadSafe)frame, blackboard);
		}

		/// <summary>
		/// Use this if the it is known that the AIParam stores specifically a Blackboard value
		/// </summary>
		public unsafe T ResolveBlackboard(FrameThreadSafe frame, AIBlackboardComponent* blackboard)
		{
			BlackboardValue blackboardValue = blackboard->GetBlackboardValue(frame, Key);
			return GetBlackboardValue(blackboardValue);
		}



		/// <summary>
		/// Use this if the it is known that the AIParam stores specifically a Config value
		/// </summary>
		public unsafe T ResolveConfig(Frame frame, AIConfig aiConfig)
		{
			return ResolveConfig((FrameThreadSafe)frame, aiConfig);
		}

		/// <summary>
		/// Use this if the it is known that the AIParam stores specifically a Config value
		/// </summary>
		public unsafe T ResolveConfig(FrameThreadSafe frame, AIConfig aiConfig)
		{
			AIConfig.KeyValuePair configPair = aiConfig != null ? aiConfig.Get(Key) : null;
			return configPair != null ? GetConfigValue(configPair) : DefaultValue;
		}



		/// <summary>
		/// Use this if the it is known that the AIParam stores specifically a Func
		/// </summary>
		public unsafe T ResolveFunction(Frame frame, EntityRef entity)
		{
			return ResolveFunction((FrameThreadSafe)frame, entity);
		}

		/// <summary>
		/// Use this if the it is known that the AIParam stores specifically a Func
		/// </summary>
		public unsafe T ResolveFunction(FrameThreadSafe frame, EntityRef entity)
		{
			return GetFunctionValue(frame, entity);
		}
	}
}
