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
			public static void Update(FrameThreadSafe frame, EntityRef entity, AIBlackboardComponent* blackboard = null, int compoundId = -1)
			{
				AIContext aiContext = new AIContext();
				Update(frame, entity, ref aiContext, blackboard, compoundId);
			}
			/// <summary>
			/// Call this method every frame to update your BT Agent.
			/// You can optionally pass a Blackboard Component to it, if your Agent uses it
			/// </summary>
			public static void Update(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext, AIBlackboardComponent* blackboard = null, int compoundId = -1)
			{
				var userParams = new BTParamsUser();
				InternalUpdate(frame, entity, ref userParams, ref aiContext, blackboard, compoundId);
			}

			/// <summary>
			/// CAUTION: Use this overload with care.<br/>
			/// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
			/// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
			/// Make sure to also implement BTParamsUser.ClearUser(frame).
			/// </summary>
			/// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
			public static void Update(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, AIBlackboardComponent* blackboard = null, int compoundId = -1)
			{
				AIContext aiContext = new AIContext();
				Update(frame, entity, ref userParams, ref aiContext, blackboard, compoundId);
			}
			/// <summary>
			/// CAUTION: Use this overload with care.<br/>
			/// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
			/// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
			/// Make sure to also implement BTParamsUser.ClearUser(frame).
			/// </summary>
			/// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
			public static void Update(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, ref AIContext aiContext, AIBlackboardComponent* blackboard = null,
				int compoundId = -1)
			{
				InternalUpdate(frame, entity, ref userParams, ref aiContext, blackboard, compoundId);
			}

			private static void InternalUpdate(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, ref AIContext aiContext, AIBlackboardComponent* blackboard = null, int compoundId = -1)
			{
				// If the user doesn't specify the desired compound agent, we update all agents, including the regular one (if applicable)
				// Otherwise, we just get the specific agent from the compound component and update it
				if (compoundId == -1)
				{
					if (frame.TryGetPointer<BTAgent>(entity, out var btAgent) == true)
					{
						BTParams btParams = new BTParams();
						btParams.SetDefaultParams(frame, btAgent, entity, false, blackboard);
						btParams.UserParams = userParams;

						btAgent->Update(ref btParams, ref aiContext);
					}

					if (frame.TryGetPointer<CompoundBTAgent>(entity, out var compoundBTAgent) == true)
					{
						var agents = frame.ResolveList<BTAgent>(compoundBTAgent->BTAgents);
						// If the user doesn't specify the desired compound agent, we update them all
						for (int i = 0; i < agents.Count; i++)
						{
							var agentPtr = agents.GetPointer(i);
							BTParams btParams = new BTParams();
							btParams.SetDefaultParams(frame, agentPtr, entity, true, blackboard);
							btParams.UserParams = userParams;

							agentPtr->Update(ref btParams, ref aiContext);
						}
					}
				}
				else if (frame.TryGetPointer<CompoundBTAgent>(entity, out var compoundBTAgent) == true)
				{
					var agents = frame.ResolveList<BTAgent>(compoundBTAgent->BTAgents);
					var agentPtr = agents.GetPointer(compoundId);
					BTParams btParams = new BTParams();
					btParams.SetDefaultParams(frame, agentPtr, entity, true, blackboard);
					btParams.UserParams = userParams;

					agentPtr->Update(ref btParams, ref aiContext);
				}
			}
		}
	}
}
