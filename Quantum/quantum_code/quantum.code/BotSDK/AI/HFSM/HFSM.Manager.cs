using Photon.Deterministic;
using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
	public static unsafe partial class HFSMManager
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public static Action<EntityRef, long, string> StateChanged;

		// ========== PUBLIC METHODS ==================================================================================

		/// <summary>
		/// Initializes the HFSM, making the current state to be equals the initial state
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Init(Frame frame, EntityRef entity, HFSMRoot root)
		{
			ThreadSafe.Init((FrameThreadSafe)frame, entity, root);
		}

		/// <summary>
		/// Initializes the HFSM, making the current state to be equals the initial state
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Init(Frame frame, HFSMData* hfsm, EntityRef entity, HFSMRoot root)
		{
			ThreadSafe.Init((FrameThreadSafe)frame, hfsm, entity, root);
		}

		/// <summary>
		/// Update the state of the HFSM.
		/// </summary>
		/// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Update(Frame frame, FP deltaTime, EntityRef entity)
		{
			ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity);
		}

		/// <summary>
		/// Update the state of the HFSM.
		/// </summary>
		/// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Update(Frame frame, FP deltaTime, HFSMData* hfsmData, EntityRef entity)
		{
			ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, hfsmData, entity);
		}


		/// <summary>
		/// Triggers an event if the target HFSM listens to it
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void TriggerEvent(Frame frame, EntityRef entity, string eventName)
		{
			ThreadSafe.TriggerEvent((FrameThreadSafe)frame, entity, eventName);
		}

		/// <summary>
		/// Triggers an event if the target HFSM listens to it
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void TriggerEvent(Frame frame, HFSMData* hfsmData, EntityRef entity, string eventName)
		{
			ThreadSafe.TriggerEvent((FrameThreadSafe)frame, hfsmData, entity, eventName);
		}

		/// <summary>
		/// Triggers an event if the target HFSM listens to it
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void TriggerEventNumber(Frame frame, HFSMData* hfsmData, EntityRef entity, Int32 eventInt)
		{
			ThreadSafe.TriggerEventNumber((FrameThreadSafe)frame, hfsmData, entity, eventInt);
		}

		// ========== INTERNAL METHODS ================================================================================

		/// <summary>
		/// Executes the On Exit actions for the current state, then changes the current state
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ChangeState(HFSMState nextState, Frame frame, HFSMData* hfsmData, EntityRef entity, string transitionId)
		{
			ThreadSafe.ChangeState(nextState, (FrameThreadSafe)frame, hfsmData, entity, transitionId);
		}
	}
}
