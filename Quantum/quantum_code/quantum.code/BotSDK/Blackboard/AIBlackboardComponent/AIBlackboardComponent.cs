using Photon.Deterministic;
using System;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct AIBlackboardComponent
	{
		#region Init/Free
		public void InitializeBlackboardComponent(Frame frame, AIBlackboard blackboardAsset)
		{
			Board = blackboardAsset;

			var assetEntries = blackboardAsset.Entries;

			if (Entries.Ptr != default)
			{
				FreeBlackboardComponent(frame);
			}

			QList<BlackboardEntry> entriesList = frame.AllocateList<BlackboardEntry>(blackboardAsset.Entries.Length);

			for (int i = 0; i < assetEntries.Length; i++)
			{
				BlackboardValue newValue = CreateValueFromEntry(assetEntries[i]);
				entriesList.Add(new BlackboardEntry { Value = newValue });
				//entriesList.Add(newValue);
			}

			Entries = entriesList;
		}

		public void FreeBlackboardComponent(Frame frame)
		{
			if (Entries.Ptr != default)
			{
				frame.FreeList(Entries);
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

			if (entry.Type == AIBlackboardValueType.AssetRef)
			{
				*newValue.AssetRefValue = default;
			}

			return newValue;
		}
		#endregion

		#region BT Specific
		public void RegisterReactiveDecorator(Frame frame, string key, BTDecorator decorator)
		{
			var blackboardEntry = GetBlackboardEntry(frame, key);

			QList<AssetRefBTDecorator> reactiveDecorators;
			// TODO: consider pre-allocating Reactive decorators so we wouldn't need to allocate it here
			// This way, this method could be threadsafe
			if (blackboardEntry->ReactiveDecorators.Ptr == default)
			{
				reactiveDecorators = frame.AllocateList<AssetRefBTDecorator>();
			}
			else
			{
				reactiveDecorators = frame.ResolveList<AssetRefBTDecorator>(blackboardEntry->ReactiveDecorators);
			}
			reactiveDecorators.Add(decorator);

			blackboardEntry->ReactiveDecorators = reactiveDecorators;
		}

		public void UnregisterReactiveDecorator(Frame frame, string key, BTDecorator decorator)
		{
			var blackboardEntry = GetBlackboardEntry(frame, key);

			if (blackboardEntry->ReactiveDecorators.Ptr != default)
			{
				QList<AssetRefBTDecorator> reactiveDecorators = frame.ResolveList<AssetRefBTDecorator>(blackboardEntry->ReactiveDecorators);
				reactiveDecorators.Remove(decorator);
				blackboardEntry->ReactiveDecorators = reactiveDecorators;
			}
		}
		#endregion

		#region Debug
		public void Dump(Frame frame)
		{
			string dumpText = "";
			var bbAsset = frame.FindAsset<AIBlackboard>(Board.Id);
			dumpText += "Blackboard Path and ID: " + bbAsset.Path + "  |  " + Board.Id.Value;

			var valuesList = frame.ResolveList(Entries);
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
				if (valuesList[i].Value.Field == BlackboardValue.ASSETREFVALUE) value = valuesList[i].Value.AssetRefValue->ToString();

				dumpText += "\nName: " + bbAsset.GetEntryName(i) + ", Value: " + value;
			}

			Log.Info(dumpText);
		}
		#endregion
	}
}
