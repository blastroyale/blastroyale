using Photon.Deterministic;
using System;

namespace Quantum
{
	public abstract unsafe partial class BTDecorator : BTNode
	{
		// ========== PUBLIC MEMBERS ==================================================================================
		[BotSDKHidden] public AssetRefBTNode Child;
		public BTAbort AbortType;

		public BTNode ChildInstance
		{
			get
			{
				return _childInstance;
			}
		}

		// ========== BTNode INTERFACE ================================================================================

		public override BTNodeType NodeType
		{
			get
			{
				return BTNodeType.Decorator;
			}
		}

		// ========== PROTECTED MEMBERS ===============================================================================

		[NonSerialized] protected BTNode _childInstance;

		// ========== BTDecorator INTERFACE ===========================================================================

		public override void OnReset(BTParams btParams, ref AIContext aiContext)
		{
			base.OnReset(btParams, ref aiContext);

			OnExit(btParams, ref aiContext);

			if (_childInstance != null)
				_childInstance.OnReset(btParams, ref aiContext);

			BTManager.OnDecoratorReset?.Invoke(btParams.Entity, Guid.Value, btParams.IsCompound);
		}

		public override void OnExit(BTParams btParams, ref AIContext aiContext)
		{
			base.OnExit(btParams, ref aiContext);
		}

		protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
		{
			if (DryRun(btParams, ref aiContext) == true)
			{
				BTManager.OnDecoratorChecked?.Invoke(btParams.Entity, Guid.Value, true, btParams.IsCompound);

				if (_childInstance != null)
				{
					var childResult = _childInstance.RunUpdate(btParams, ref aiContext);
					if (childResult == BTStatus.Abort)
					{
						EvaluateAbortNode(btParams);
						SetStatus(btParams.FrameThreadSafe, BTStatus.Abort, btParams.Agent);
						return BTStatus.Abort;
					}

					return childResult;
				}

				return BTStatus.Success;
			}

			BTManager.OnDecoratorChecked?.Invoke(btParams.Entity, Guid.Value, false, btParams.IsCompound);

			return BTStatus.Failure;
		}

		public override bool OnDynamicRun(BTParams btParams, ref AIContext aiContext)
		{
			var result = DryRun(btParams, ref aiContext);
			if (result == false)
			{
				return false;
			}
			else if (ChildInstance.NodeType != BTNodeType.Decorator)
			{
				return true;
			}
			else
			{
				return ChildInstance.OnDynamicRun(btParams, ref aiContext);
			}
		}

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			// Cache the child
			_childInstance = (BTNode)resourceManager.GetAsset(Child.Id);
			_childInstance.Parent = this;
		}
	}
}