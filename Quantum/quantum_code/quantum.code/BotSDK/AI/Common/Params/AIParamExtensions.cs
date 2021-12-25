namespace Quantum
{
	public static unsafe partial class AIParamExtensions
	{
		public static T ResolveFromHFSM<T>(this AIParam<T> aiParam, Frame frame, EntityRef entity)
		{
			var aiConfigRef = aiParam.Source == AIParamSource.Config
				? frame.Unsafe.GetPointer<HFSMAgent>(entity)->Config
				: default;

			return aiParam.Resolve(frame, entity, aiConfigRef);
		}

		public static T ResolveFromHFSM<T>(this AIParam<T> aiParam, FrameThreadSafe frame, EntityRef entity)
		{
			var aiConfigRef = aiParam.Source == AIParamSource.Config
				? frame.GetPointer<HFSMAgent>(entity)->Config
				: default;

			return aiParam.Resolve(frame, entity, aiConfigRef);
		}

		// ------------------------------------

		public static T ResolveFromGOAP<T>(this AIParam<T> aiParam, Frame frame, EntityRef entity)
		{
			var aiConfigRef = aiParam.Source == AIParamSource.Config
				? frame.Unsafe.GetPointer<GOAPAgent>(entity)->Config
				: default;

			return aiParam.Resolve(frame, entity, aiConfigRef);
		}

		public static T ResolveFromGOAP<T>(this AIParam<T> aiParam, FrameThreadSafe frame, EntityRef entity)
		{
			var aiConfigRef = aiParam.Source == AIParamSource.Config
				? frame.GetPointer<GOAPAgent>(entity)->Config
				: default;

			return aiParam.Resolve(frame, entity, aiConfigRef);
		}

		// ------------------------------------

		public static T ResolveFromBT<T>(this AIParam<T> aiParam, Frame frame, EntityRef entity)
		{
			var aiConfigRef = aiParam.Source == AIParamSource.Config
				? frame.Unsafe.GetPointer<BTAgent>(entity)->Config
				: default;

			return aiParam.Resolve(frame, entity, aiConfigRef);
		}

		public static T ResolveFromBT<T>(this AIParam<T> aiParam, FrameThreadSafe frame, EntityRef entity)
		{
			var aiConfigRef = aiParam.Source == AIParamSource.Config
				? frame.GetPointer<BTAgent>(entity)->Config
				: default;

			return aiParam.Resolve(frame, entity, aiConfigRef);
		}

		// ------------------------------------

		public static T Resolve<T>(this AIParam<T> aiParam, Frame frame, EntityRef entity, AssetRefAIConfig aiConfigRef)
		{
			var blackboard = aiParam.Source == AIParamSource.Blackboard
				? frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)
				: null;

			var aiConfig = aiParam.Source == AIParamSource.Config
				? frame.FindAsset<AIConfig>(aiConfigRef.Id)
				: null;

			return aiParam.Resolve(frame, entity, blackboard, aiConfig);
		}

		public static T Resolve<T>(this AIParam<T> aiParam, FrameThreadSafe frame, EntityRef entity, AssetRefAIConfig aiConfigRef)
		{
			var blackboard = aiParam.Source == AIParamSource.Blackboard
				? frame.GetPointer<AIBlackboardComponent>(entity)
				: null;

			var aiConfig = aiParam.Source == AIParamSource.Config
				? frame.FindAsset<AIConfig>(aiConfigRef.Id)
				: null;

			return aiParam.Resolve(frame, entity, blackboard, aiConfig);
		}
	}
}