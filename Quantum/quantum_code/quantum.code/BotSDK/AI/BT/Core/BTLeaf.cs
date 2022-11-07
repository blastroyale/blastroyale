using Photon.Deterministic;
using System;

namespace Quantum
{
	public unsafe abstract partial class BTLeaf : BTNode
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AssetRefBTService[] Services;
		public BTService[] ServiceInstances
		{
			get
			{
				return _serviceInstances;
			}
		}

		// ========== BTNode INTERFACE ================================================================================

		public override BTNodeType NodeType
		{
			get
			{
				return BTNodeType.Leaf;
			}
		}

		// ========== PROTECTED MEMBERS ===============================================================================

		[NonSerialized] protected BTService[] _serviceInstances;

		// ========== BTDecorator INTERFACE ===========================================================================

		public override unsafe void Init(FrameThreadSafe frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			base.Init(frame, blackboard, agent);

			for (int i = 0; i < Services.Length; i++)
			{
				BTService service = frame.FindAsset<BTService>(Services[i].Id);
				service.Init(frame, agent, blackboard);
			}
		}

		public override void OnEnterRunning(BTParams btParams, ref AIContext aiContext)
		{
			var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRefBTService>(btParams.Agent->ActiveServices);
			for (int i = 0; i < _serviceInstances.Length; i++)
			{
				_serviceInstances[i].OnEnter(btParams, ref aiContext);
				activeServicesList.Add(Services[i]);
			}
		}

		public override void OnEnter(BTParams btParams, ref AIContext aiContext)
		{
			base.OnEnter(btParams, ref aiContext);

			if (btParams.IsCompound == false)
			{
				BTManager.OnNodeEnter?.Invoke(btParams.Entity, Guid.Value, btParams.IsCompound);
			}
		}

		public override void OnExit(BTParams btParams, ref AIContext aiContext)
		{
			var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRefBTService>(btParams.Agent->ActiveServices);
			for (Int32 i = 0; i < _serviceInstances.Length; i++)
			{
				activeServicesList.Remove(Services[i]);
			}

			BTManager.OnNodeExit?.Invoke(btParams.Entity, Guid.Value, btParams.IsCompound);
		}

		public override void OnReset(BTParams btParams, ref AIContext aiContext)
		{
			base.OnReset(btParams, ref aiContext);
			OnExit(btParams, ref aiContext);
		}

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			// Cache the service assets links
			_serviceInstances = new BTService[Services.Length];
			for (int i = 0; i < Services.Length; i++)
			{
				_serviceInstances[i] = (BTService)resourceManager.GetAsset(Services[i].Id);
			}
		}
	}
}