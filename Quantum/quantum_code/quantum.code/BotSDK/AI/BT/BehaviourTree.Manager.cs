using Photon.Deterministic;
using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
	public static unsafe partial class BTManager
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public static Action<EntityRef, string> OnSetupDebugger;

		public static Action<EntityRef, long> OnNodeEnter;
		public static Action<EntityRef, long> OnNodeExit;
		public static Action<EntityRef, long> OnNodeSuccess;
		public static Action<EntityRef, long> OnNodeFailure;
		public static Action<EntityRef, long, bool> OnDecoratorChecked;
		public static Action<EntityRef, long> OnDecoratorReset;
		public static Action<EntityRef> OnTreeCompleted;

		/// <summary>
		/// Call this once, to initialize the BTAgent.
		/// This method internally looks for a Blackboard Component on the entity
		/// and passes it down the pipeline.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="entity"></param>
		/// <param name="root"></param>
		public static void Init(Frame frame, EntityRef entity, BTRoot root)
		{
			if (frame.Unsafe.TryGetPointer(entity, out BTAgent* agent))
			{
				agent->Initialize(frame, entity, agent, root, true);
			}
			else
			{
				Log.Error("[Bot SDK] Tried to initialize an entity which has no BTAgent component");
			}
		}

		/// <summary>
		/// Made for internal use only.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearBTParams(BTParams btParams)
		{
			btParams.Reset();
		}

		/// <summary>
		/// Call this method every frame to update your BT Agent.
		/// You can optionally pass a Blackboard Component to it, if your Agent uses it
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Update(Frame frame, EntityRef entity, AIBlackboardComponent* blackboard = null)
		{
			ThreadSafe.Update((FrameThreadSafe)frame, entity, blackboard);
		}

		/// <summary>
		/// CAUTION: Use this overload with care.<br/>
		/// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
		/// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
		/// Make sure to also implement BTParamsUser.ClearUser(frame).
		/// </summary>
		/// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Update(Frame frame, EntityRef entity, ref BTParamsUser userParams, AIBlackboardComponent* blackboard = null)
		{
			ThreadSafe.Update((FrameThreadSafe)frame, entity, ref userParams, blackboard);
		}
	}
}
