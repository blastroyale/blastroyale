using Photon.Deterministic;
using System;

namespace Quantum
{
	public static unsafe partial class BTManager
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public class ThreadSafe
		{
			/// <summary>
			/// Made for internal use only.
			/// </summary>
			public static void ClearBTParams(BTParams btParams)
			{
				btParams.Reset();
			}

			/// <summary>
			/// Call this method every frame to update your BT Agent.
			/// You can optionally pass a Blackboard Component to it, if your Agent uses it
			/// </summary>
			public static void Update(FrameThreadSafe frame, EntityRef entity, AIBlackboardComponent* blackboard = null)
			{
				var agent = frame.GetPointer<BTAgent>(entity);
				BTParams btParams = new BTParams();
				btParams.SetDefaultParams(frame, agent, entity, blackboard);

				agent->Update(ref btParams);
			}

			/// <summary>
			/// CAUTION: Use this overload with care.<br/>
			/// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
			/// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
			/// Make sure to also implement BTParamsUser.ClearUser(frame).
			/// </summary>
			/// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
			public static void Update(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, AIBlackboardComponent* blackboard = null)
			{
				var agent = frame.GetPointer<BTAgent>(entity);
				BTParams btParams = new BTParams();
				btParams.SetDefaultParams(frame, agent, entity, blackboard);
				btParams.UserParams = userParams;

				agent->Update(ref btParams);
			}
		}
	}
}
