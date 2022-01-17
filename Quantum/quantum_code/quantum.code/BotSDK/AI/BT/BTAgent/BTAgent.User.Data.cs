using Photon.Deterministic;
using System;

namespace Quantum
{
	public unsafe partial struct BTAgent
	{
		#region Int and FP Data
		// Getter / Setters of node FP and Int32 data
		public void AddFPData(Frame frame, FP fpValue)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			BTDataValue newDataValue = new BTDataValue();
			*newDataValue.FPValue = fpValue;
			nodesDataList.Add(newDataValue);
		}

		public void AddIntData(Frame frame, Int32 intValue)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			BTDataValue newDataValue = new BTDataValue();
			*newDataValue.IntValue = intValue;
			nodesDataList.Add(newDataValue);
		}

		public void SetFPData(Frame frame, FP value, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			*nodesDataList.GetPointer(index)->FPValue = value;
		}

		public void SetIntData(Frame frame, Int32 value, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			*nodesDataList.GetPointer(index)->IntValue = value;
		}

		public FP GetFPData(Frame frame, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			return *nodesDataList.GetPointer(index)->FPValue;
		}

		public Int32 GetIntData(Frame frame, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			return *nodesDataList.GetPointer(index)->IntValue;
		}
		#endregion

		// -- THREADSAFE

		#region THREADSAFE Int and FP Data
		// Getter / Setters of node FP and Int32 data
		public void AddFPData(FrameThreadSafe frame, FP fpValue)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			BTDataValue newDataValue = new BTDataValue();
			*newDataValue.FPValue = fpValue;
			nodesDataList.Add(newDataValue);
		}

		public void AddIntData(FrameThreadSafe frame, Int32 intValue)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			BTDataValue newDataValue = new BTDataValue();
			*newDataValue.IntValue = intValue;
			nodesDataList.Add(newDataValue);
		}

		public void SetFPData(FrameThreadSafe frame, FP value, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			*nodesDataList.GetPointer(index)->FPValue = value;
		}

		public void SetIntData(FrameThreadSafe frame, Int32 value, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			*nodesDataList.GetPointer(index)->IntValue = value;
		}

		public FP GetFPData(FrameThreadSafe frame, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			return *nodesDataList.GetPointer(index)->FPValue;
		}

		public Int32 GetIntData(FrameThreadSafe frame, Int32 index)
		{
			var nodesDataList = frame.ResolveList<BTDataValue>(BTDataValues);
			return *nodesDataList.GetPointer(index)->IntValue;
		}
		#endregion
	}
}