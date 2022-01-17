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
			public AssetRef AsAssetRef;
		}

		// ============================================================================================================

		[Serializable]
		public struct AIBlackboardInitialValueEntry
		{
			public string Key;
			public AIBlackboardInitialValue Value;
		}

		// ============================================================================================================

		// ========== PUBLIC MEMBERS ==================================================================================

		public bool ReportMissingEntries = true;

		public AssetRefAIBlackboard AIBlackboard;
		public AIBlackboardInitialValueEntry[] InitialValues;

		// ========== PUBLIC METHODS ==================================================================================

		public unsafe static void InitializeBlackboard(Frame frame, AIBlackboardComponent* blackboard, AIBlackboardInitializer blackboardInitializer, AIBlackboardInitialValueEntry[] blackboardOverrides = null)
		{
			AIBlackboard board = frame.FindAsset<AIBlackboard>(blackboardInitializer.AIBlackboard.Id);

			blackboard->InitializeBlackboardComponent(frame, board);

			ApplyEntries(frame, blackboard, blackboardInitializer, blackboardInitializer.InitialValues);
			ApplyEntries(frame, blackboard, blackboardInitializer, blackboardOverrides);
		}

		public unsafe static void ApplyEntries(Frame frame, AIBlackboardComponent* blackboard, AIBlackboardInitializer blackboardInitializer, AIBlackboardInitialValueEntry[] values)
		{
			if (values == null) return;

			for (int i = 0; i < values.Length; i++)
			{
				string key = values[i].Key;
				if (blackboard->HasEntry(frame, key) == false)
				{
					if (blackboardInitializer.ReportMissingEntries)
					{
						Quantum.Log.Warn($"Blackboard {blackboard->Board} does not have an entry with a key called '{key}'");
					}
					continue;
				}

				BlackboardValue value = blackboard->GetBlackboardValue(frame, key);
				switch (value.Field)
				{
					case BlackboardValue.BOOLEANVALUE:
						blackboard->Set(frame, key, values[i].Value.AsBoolean);
						break;
					case BlackboardValue.BYTEVALUE:
						blackboard->Set(frame, key, values[i].Value.AsByte);
						break;
					case BlackboardValue.ENTITYREFVALUE:
						blackboard->Set(frame, key, values[i].Value.AsEntityRef);
						break;
					case BlackboardValue.FPVALUE:
						blackboard->Set(frame, key, values[i].Value.AsFP);
						break;
					case BlackboardValue.INTEGERVALUE:
						blackboard->Set(frame, key, values[i].Value.AsInteger);
						break;
					case BlackboardValue.FPVECTOR2VALUE:
						blackboard->Set(frame, key, values[i].Value.AsFPVector2);
						break;
					case BlackboardValue.FPVECTOR3VALUE:
						blackboard->Set(frame, key, values[i].Value.AsFPVector3);
						break;
					case BlackboardValue.ASSETREFVALUE:
						blackboard->Set(frame, key, values[i].Value.AsAssetRef);
						break;
				}
			}
		}
	}
}
