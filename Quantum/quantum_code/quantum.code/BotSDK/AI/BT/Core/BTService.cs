using Photon.Deterministic;
using System;

namespace Quantum
{
	public unsafe abstract partial class BTService
	{
		// ========== PUBLIC MEMBERS ==================================================================================
		public FP IntervalInSec;
		public bool RunOnEnter;

		[BotSDKHidden] public Int32 Id;

		// ========== BTService INTERFACE =============================================================================

		public virtual void Init(FrameThreadSafe frame, BTAgent* agent, AIBlackboardComponent* blackboard)
		{
			var endTimesList = frame.ResolveList<FP>(agent->ServicesEndTimes);
			endTimesList.Add(0);
		}

		public virtual void RunUpdate(BTParams btParams)
		{
			var endTime = GetEndTime(btParams.FrameThreadSafe, btParams.Agent);
			if (btParams.Frame.BotSDKGameTime() >= endTime)
			{
				OnUpdate(btParams);
				SetEndTime(btParams.FrameThreadSafe, btParams.Agent);
			}
		}

		public virtual void OnEnter(BTParams btParams)
		{
			SetEndTime(btParams.FrameThreadSafe, btParams.Agent);

			if(RunOnEnter == true)
			{
				OnUpdate(btParams);
			}
		}

		/// <summary>
		/// Called whenever the Service is part of the current subtree
		/// and its waiting time is already over
		/// </summary>
		protected abstract void OnUpdate(BTParams btParams);

		// ========== PUBLIC METHODS ==================================================================================

		public void SetEndTime(FrameThreadSafe frame, BTAgent* agent)
		{
			var endTimesList = frame.ResolveList<FP>(agent->ServicesEndTimes);
			endTimesList[Id] = ((Frame)frame).BotSDKGameTime() + IntervalInSec;
		}

		public FP GetEndTime(FrameThreadSafe frame, BTAgent* agent)
		{
			var endTime = frame.ResolveList(agent->ServicesEndTimes);
			return endTime[Id];
		}

		public static void TickServices(BTParams btParams)
		{
			var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRefBTService>(btParams.Agent->ActiveServices);

			for (int i = 0; i < activeServicesList.Count; i++)
			{
				var service = btParams.FrameThreadSafe.FindAsset<BTService>(activeServicesList[i].Id);
				try
				{
					service.RunUpdate(btParams);
				}
				catch (Exception e)
				{
					Log.Error("Exception in Behaviour Tree service '{0}' ({1}) - setting node status to Failure", service.GetType().ToString(), service.Guid);
					Log.Exception(e);
				}
			}
		}
	}
}
