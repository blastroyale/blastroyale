using Photon.Deterministic;
using System;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct AIBlackboardComponent
	{
		#region Getters
		public QBoolean GetBoolean(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.BooleanValue;
		}

		public byte GetByte(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.ByteValue;
		}

		public Int32 GetInteger(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.IntegerValue;
		}

		public FP GetFP(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.FPValue;
		}

		public FPVector2 GetVector2(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.FPVector2Value;
		}

		public FPVector3 GetVector3(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.FPVector3Value;
		}

		public EntityRef GetEntityRef(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.EntityRefValue;
		}

		public AssetRef GetAssetRef(Frame frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.AssetRefValue;
		}
		#endregion

		#region Setters
		public BlackboardEntry* Set(Frame frame, string key, QBoolean value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.BooleanValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(Frame frame, string key, byte value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.ByteValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(Frame frame, string key, Int32 value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.IntegerValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(Frame frame, string key, FP value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.FPValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(Frame frame, string key, FPVector2 value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.FPVector2Value = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(Frame frame, string key, FPVector3 value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.FPVector3Value = value;

			return valueList.GetPointer(ID);

		}

		public BlackboardEntry* Set(Frame frame, string key, EntityRef value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.EntityRefValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(Frame frame, string key, AssetRef value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.AssetRefValue = value;

			return valueList.GetPointer(ID);
		}
		#endregion

		#region Helpers
		public BlackboardEntry* GetBlackboardEntry(Frame frame, string key)
		{
			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			var ID = bbAsset.GetEntryID(key);
			var values = frame.ResolveList(Entries);
			return values.GetPointer(ID);
		}

		public BlackboardValue GetBlackboardValue(Frame frame, string key)
		{
			Assert.Check(string.IsNullOrEmpty(key) == false, "The Key cannot be empty or null.");

			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			var ID = bbAsset.GetEntryID(key);
			var values = frame.ResolveList(Entries);

			return values[ID].Value;
		}

		public Int32 GetID(Frame frame, string key)
		{
			Assert.Check(string.IsNullOrEmpty(key) == false, "The Key cannot be empty or null.");

			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			var ID = bbAsset.GetEntryID(key);

			return ID;
		}
		
		public bool TryGetID(Frame f, string key, out Int32 id)
		{
			var bbAsset = f.FindAsset<AIBlackboard>(Board.Id);

			return bbAsset.TryGetEntryID(key, out id);
		}

		public bool HasEntry(Frame frame, string key)
		{
			var boardAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			return boardAsset.HasEntry(key);
		}
		#endregion

		// -- THREADSAFE

		#region Getters
		public QBoolean GetBoolean(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.BooleanValue;
		}

		public byte GetByte(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.ByteValue;
		}

		public Int32 GetInteger(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.IntegerValue;
		}

		public FP GetFP(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.FPValue;
		}

		public FPVector2 GetVector2(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.FPVector2Value;
		}

		public FPVector3 GetVector3(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.FPVector3Value;
		}

		public EntityRef GetEntityRef(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.EntityRefValue;
		}

		public AssetRef GetAssetRef(FrameThreadSafe frame, string key)
		{
			var bbValue = GetBlackboardValue(frame, key);
			return *bbValue.AssetRefValue;
		}
		#endregion

		#region Setters
		public BlackboardEntry* Set(FrameThreadSafe frame, string key, QBoolean value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.BooleanValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, byte value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.ByteValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, Int32 value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.IntegerValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, FP value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.FPValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, FPVector2 value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.FPVector2Value = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, FPVector3 value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.FPVector3Value = value;

			return valueList.GetPointer(ID);

		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, EntityRef value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.EntityRefValue = value;

			return valueList.GetPointer(ID);
		}

		public BlackboardEntry* Set(FrameThreadSafe frame, string key, AssetRef value)
		{
			QList<BlackboardEntry> valueList = frame.ResolveList(Entries);
			var ID = GetID(frame, key);
			*valueList.GetPointer(ID)->Value.AssetRefValue = value;

			return valueList.GetPointer(ID);
		}
		#endregion

		#region Helpers
		public BlackboardEntry* GetBlackboardEntry(FrameThreadSafe frame, string key)
		{
			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			var ID = bbAsset.GetEntryID(key);
			var values = frame.ResolveList(Entries);
			return values.GetPointer(ID);
		}

		public BlackboardValue GetBlackboardValue(FrameThreadSafe frame, string key)
		{
			Assert.Check(string.IsNullOrEmpty(key) == false, "The Key cannot be empty or null.");

			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			var ID = bbAsset.GetEntryID(key);
			var values = frame.ResolveList(Entries);

			return values[ID].Value;
		}

		public Int32 GetID(FrameThreadSafe frame, string key)
		{
			Assert.Check(string.IsNullOrEmpty(key) == false, "The Key cannot be empty or null.");

			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			var ID = bbAsset.GetEntryID(key);

			return ID;
		}

		public bool HasEntry(FrameThreadSafe frame, string key)
		{
			var boardAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			return boardAsset.HasEntry(key);
		}
		#endregion
	}
}
