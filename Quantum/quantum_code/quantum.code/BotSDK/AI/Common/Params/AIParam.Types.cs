using Photon.Deterministic;
using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
	[System.Serializable]
	public unsafe sealed class AIParamInt : AIParam<int>
	{
		public static implicit operator AIParamInt(int value) { return new AIParamInt() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<int> _cachedFunction;

		protected override int GetBlackboardValue(BlackboardValue value)
		{
			return *value.IntegerValue;
		}

		protected override int GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.Integer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override int GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override int GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<int>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamBool : AIParam<bool>
	{
		public static implicit operator AIParamBool(bool value) { return new AIParamBool() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<bool> _cachedFunction;

		protected override bool GetBlackboardValue(BlackboardValue value)
		{
			return *value.BooleanValue;
		}

		protected override bool GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.Boolean;
		}

		protected override bool GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override bool GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<bool>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamByte : AIParam<byte>
	{
		public static implicit operator AIParamByte(byte value) { return new AIParamByte() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<byte> _cachedFunction;

		protected override byte GetBlackboardValue(BlackboardValue value)
		{
			return *value.ByteValue;
		}

		protected override byte GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.Byte;
		}

		protected override byte GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override byte GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<byte>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamFP : AIParam<FP>
	{
		public static implicit operator AIParamFP(FP value) { return new AIParamFP() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<FP> _cachedFunction;

		protected override FP GetBlackboardValue(BlackboardValue value)
		{
			return *value.FPValue;
		}

		protected override FP GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.FP;
		}

		protected override FP GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override FP GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<FP>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamFPVector2 : AIParam<FPVector2>
	{
		public static implicit operator AIParamFPVector2(FPVector2 value) { return new AIParamFPVector2() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<FPVector2> _cachedFunction;

		protected override FPVector2 GetBlackboardValue(BlackboardValue value)
		{
			return *value.FPVector2Value;
		}

		protected override FPVector2 GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.FPVector2;
		}

		protected override FPVector2 GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override FPVector2 GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<FPVector2>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamFPVector3 : AIParam<FPVector3>
	{
		public static implicit operator AIParamFPVector3(FPVector3 value) { return new AIParamFPVector3() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<FPVector3> _cachedFunction;

		protected override FPVector3 GetBlackboardValue(BlackboardValue value)
		{
			return *value.FPVector3Value;
		}

		protected override FPVector3 GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.FPVector3;
		}

		protected override FPVector3 GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override FPVector3 GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<FPVector3>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamString : AIParam<string>
	{
		public static implicit operator AIParamString(string value) { return new AIParamString() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<string> _cachedFunction;

		protected override string GetBlackboardValue(BlackboardValue value)
		{
			throw new NotSupportedException("Blackboard variables as strings are not supported.");
		}

		protected override string GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.String;
		}

		protected override string GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override string GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<string>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamEntityRef : AIParam<EntityRef>
	{
		public static implicit operator AIParamEntityRef(EntityRef value) { return new AIParamEntityRef() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<EntityRef> _cachedFunction;

		protected override EntityRef GetBlackboardValue(BlackboardValue value)
		{
			return *value.EntityRefValue;
		}

		protected override EntityRef GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.EntityRef;
		}

		protected override EntityRef GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override EntityRef GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<EntityRef>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}

	[System.Serializable]
	public unsafe sealed class AIParamAssetRef : AIParam<AssetRef>
	{
		public static implicit operator AIParamAssetRef(AssetRef value) { return new AIParamAssetRef() { DefaultValue = value }; }

		public AssetRefAIFunction FunctionRef;

		[NonSerialized] private AIFunction<AssetRef> _cachedFunction;

		protected override AssetRef GetBlackboardValue(BlackboardValue value)
		{
			return *value.AssetRefValue;
		}

		protected override AssetRef GetConfigValue(AIConfig.KeyValuePair configPair)
		{
			return configPair.Value.AssetRef;
		}

		protected override AssetRef GetFunctionValue(Frame frame, EntityRef entity)
		{
			return GetFunctionValue((FrameThreadSafe)frame, entity);
		}

		protected override AssetRef GetFunctionValue(FrameThreadSafe frame, EntityRef entity)
		{
			if (_cachedFunction == null)
			{
				_cachedFunction = frame.FindAsset<AIFunction<AssetRef>>(FunctionRef.Id);
			}

			return _cachedFunction.Execute(frame, entity);
		}
	}
}
