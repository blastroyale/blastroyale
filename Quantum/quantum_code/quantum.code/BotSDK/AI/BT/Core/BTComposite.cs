using Photon.Deterministic;
using System;

namespace Quantum
{
	public unsafe abstract partial class BTComposite : BTNode
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AssetRefBTNode[] Children;
		public AssetRefBTService[] Services;
		[BotSDKHidden] public AssetRefBTNode TopmostDecorator;

		public BTDataIndex CurrentChildIndex;

		public bool IsDynamic;

		public BTNode[] ChildInstances
		{
			get
			{
				return _childInstances;
			}
		}

		public BTService[] ServiceInstances
		{
			get
			{
				return _serviceInstances;
			}
		}

		public override BTNodeType NodeType
		{
			get
			{
				return BTNodeType.Composite;
			}
		}

		// ========== PROTECTED MEMBERS ===============================================================================

		protected BTNode[] _childInstances;
		protected BTService[] _serviceInstances;
		protected BTNode _topmostDecoratorInstance;

		// ========== BTNode INTERFACE ================================================================================

		public override void Init(FrameThreadSafe frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			base.Init(frame, blackboard, agent);

			agent->AddIntData(frame, 0);

			for (Int32 i = 0; i < Services.Length; i++)
			{
				BTService service = frame.FindAsset<BTService>(Services[i].Id);
				service.Init(frame, agent, blackboard);
			}
		}

		public override void OnEnter(BTParams btParams)
		{
			BTManager.OnNodeEnter?.Invoke(btParams.Entity, Guid.Value);
			SetCurrentChild(btParams.FrameThreadSafe, 0, btParams.Agent);
		}

		public override void OnEnterRunning(BTParams btParams)
		{
			var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRefBTService>(btParams.Agent->ActiveServices);

			for (Int32 i = 0; i < _serviceInstances.Length; i++)
			{
				_serviceInstances[i].OnEnter(btParams);

				activeServicesList.Add(Services[i]);
			}

			if (IsDynamic == true)
			{
				var dynamicComposites = btParams.FrameThreadSafe.ResolveList<AssetRefBTComposite>(btParams.Agent->DynamicComposites);
				dynamicComposites.Add(this);
			}
		}

		public override void OnReset(BTParams btParams)
		{
			base.OnReset(btParams);

			OnExit(btParams);

			for (Int32 i = 0; i < _childInstances.Length; i++)
				_childInstances[i].OnReset(btParams);
		}

		public override void OnExit(BTParams btParams)
		{
			base.OnExit(btParams);

			BTManager.OnNodeExit?.Invoke(btParams.Entity, Guid.Value);

			var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRefBTService>(btParams.Agent->ActiveServices);
			for (Int32 i = 0; i < _serviceInstances.Length; i++)
			{
				activeServicesList.Remove(Services[i]);
			}

			if (IsDynamic == true)
			{
				var dynamicComposites = btParams.FrameThreadSafe.ResolveList<AssetRefBTComposite>(btParams.Agent->DynamicComposites);
				dynamicComposites.Remove(this);
			}
		}

		public override bool OnDynamicRun(BTParams btParams)
		{
			if (_topmostDecoratorInstance != null)
			{
				return _topmostDecoratorInstance.OnDynamicRun(btParams);
			}

			return true;
		}

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			// Cache the child assets links
			_childInstances = new BTNode[Children.Length];
			for (Int32 i = 0; i < Children.Length; i++)
			{
				_childInstances[i] = (BTNode)resourceManager.GetAsset(Children[i].Id);
				_childInstances[i].Parent = this;
				_childInstances[i].ParentIndex = i;
			}

			// Cache the service assets links
			_serviceInstances = new BTService[Services.Length];
			for (Int32 i = 0; i < Services.Length; i++)
			{
				_serviceInstances[i] = (BTService)resourceManager.GetAsset(Services[i].Id);
			}

			if (TopmostDecorator != null)
			{
				_topmostDecoratorInstance = (BTDecorator)resourceManager.GetAsset(TopmostDecorator.Id);
			}
		}

		// ========== PUBLIC METHODS ==================================================================================

		public void AbortNodes(BTParams btParams, Int32 firstIndex = 0)
		{
			for (int i = firstIndex; i < _childInstances.Length; i++)
			{
				_childInstances[i].SetStatus(btParams.FrameThreadSafe, BTStatus.Abort, btParams.Agent);
			}
		}

		// ========== INTERNAL METHODS ================================================================================

		internal Int32 GetCurrentChild(FrameThreadSafe frame, BTAgent* agent)
		{
			Byte currentChild = (Byte)agent->GetIntData(frame, CurrentChildIndex.Index);
			return currentChild;
		}

		internal void SetCurrentChild(FrameThreadSafe frame, Int32 currentIndex, BTAgent* agent)
		{
			agent->SetIntData(frame, currentIndex, CurrentChildIndex.Index);
		}


		// ========== BTComposite INTERFACE ===========================================================================

		/// <summary>
		/// When a Composite node is Updated, it only increase the current child updated
		/// when the child results in either FAIL/SUCCESS. So we need this callback
		/// to be used when the child was RUNNING and then had some result, to properly increase the current 
		/// child ID
		/// </summary>
		/// <param name="btParams"></param>
		/// <param name="childResult"></param>
		internal virtual void ChildCompletedRunning(BTParams btParams, BTStatus childResult)
		{
		}
	}
}