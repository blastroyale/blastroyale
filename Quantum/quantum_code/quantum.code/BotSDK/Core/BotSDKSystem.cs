using System;

namespace Quantum
{
	/// <summary>
	/// Using this system is optional. It is only used to aim the Debugger on the Unity side.
	/// It is also safe to copy logic from this system into your own systems, if it better suits your architecture.
	/// </summary>
	public unsafe class BotSDKSystem : SystemSignalsOnly, ISignalOnComponentAdded<HFSMAgent>,
																												ISignalOnComponentAdded<BTAgent>, ISignalOnComponentRemoved<BTAgent>,
																												ISignalOnComponentAdded<UTAgent>, ISignalOnComponentRemoved<UTAgent>,
																												ISignalOnComponentAdded<GOAPAgent>, ISignalOnComponentRemoved<GOAPAgent>,
																												ISignalOnComponentRemoved<AIBlackboardComponent>
	{
		// ========== HFSM ============================================================================================

		public void OnAdded(Frame frame, EntityRef entity, HFSMAgent* component)
		{
			HFSMData* hfsmData = &component->Data;
			if (hfsmData->Root == default)
				return;

			HFSMRoot rootAsset = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);
			HFSMManager.Init(frame, entity, rootAsset);
		}

		// ========== BT ============================================================================================

		public void OnAdded(Frame frame, EntityRef entity, BTAgent* component)
		{
			// Mainly used to automatically initialize entity prototypes
			// If the prototype's Tree reference is not default and the BTAgent
			// is not initialized yet, then it is initialized here;
			if (component->Tree != default)
			{
				component->Initialize(frame, entity, component, component->Tree, false);
			}
		}

		public void OnRemoved(Frame frame, EntityRef entity, BTAgent* component)
		{
			component->Free(frame);
		}

		// ========== UT ============================================================================================

		public void OnAdded(Frame frame, EntityRef entity, UTAgent* component)
		{
			UTManager.Init(frame, &component->UtilityReasoner, component->UtilityReasoner.UTRoot, entity);
		}

		public void OnRemoved(Frame frame, EntityRef entity, UTAgent* component)
		{
			component->UtilityReasoner.Free(frame);
		}

		// ========== GOAP ============================================================================================

		void ISignalOnComponentAdded<GOAPAgent>.OnAdded(Frame frame, EntityRef entity, GOAPAgent* component)
		{
			if (component->Root == default)
				return;

			var rootAsset = frame.FindAsset<GOAPRoot>(component->Root.Id);
			GOAPManager.Initialize(frame, entity, rootAsset);
		}

		void ISignalOnComponentRemoved<GOAPAgent>.OnRemoved(Frame frame, EntityRef entity, GOAPAgent* component)
		{
			GOAPManager.Deinitialize(frame, entity);
		}

		// ========== BLACKBOARD ============================================================================================

		public void OnRemoved(Frame frame, EntityRef entity, AIBlackboardComponent* component)
		{
			component->FreeBlackboardComponent(frame);
		}
	}
}
