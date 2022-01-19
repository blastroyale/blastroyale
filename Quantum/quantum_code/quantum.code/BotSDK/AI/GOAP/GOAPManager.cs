using System;
using Photon.Deterministic;
using System.Collections.Generic;

namespace Quantum
{
	public static unsafe class GOAPManager
	{
		// PUBLIC MEMBERS

		public static EntityRef DebugEntity;

		// PRIVATE MEMBERS

		private static GOAPAStar.HeuristicCost _heuristicCost;

		// PUBLIC METHODS

		public static void Initialize(Frame frame, EntityRef entity, GOAPRoot root)
		{
			var agent = frame.Unsafe.GetPointer<GOAPAgent>(entity);
			agent->Root = root;

			var disableTimes = frame.AllocateList<FP>(root.Goals.Length);
			for (int i = 0; i < root.GoalRefs.Length; i++)
			{
				disableTimes.Add(0);
			}

			agent->GoalDisableTimes = disableTimes;

			PrepareHeuristicCost(frame, null);
		}

		public static void Deinitialize(Frame frame, EntityRef entity)
		{
			var agent = frame.Unsafe.GetPointer<GOAPAgent>(entity);

			agent->Root = default;

			frame.FreeList(agent->GoalDisableTimes);
			agent->GoalDisableTimes = default;
		}

		public static void Update(Frame frame, EntityRef entity, FP deltaTime)
		{
			if (_heuristicCost == null)
			{
				// We are trying to update GOAP without proper initialization, this can happen after late joins
				frame.Unsafe.TryGetPointerSingleton(out GOAPData* goapData);
				PrepareHeuristicCost(frame, goapData != null ? frame.FindAsset<GOAPHeuristic>(goapData->HeuristicCost.Id) : null);
			}

			var context = GetContext(frame, entity);
			var agent = context.Agent;

			bool debug = DebugEntity == entity;

			// Update disable times
			var goalDisableTimes = frame.ResolveList(agent->GoalDisableTimes);
			for (int i = 0; i < goalDisableTimes.Count; i++)
			{
				goalDisableTimes[i] = FPMath.Max(FP._0, goalDisableTimes[i] - deltaTime);
			}

			var currentGoal = agent->CurrentGoal.Id.IsValid == true ? frame.FindAsset<GOAPGoal>(agent->CurrentGoal.Id) : null;
			var currentAction = GetCurrentAction(frame, agent);

			if (currentGoal != null)
			{
				// Decrease interruption timer
				agent->InterruptionCheckCooldown = FPMath.Max(agent->InterruptionCheckCooldown - deltaTime, 0);

				if (currentGoal.HasFinished(frame, context) == true)
				{
					StopCurrentGoal(frame, context, ref currentGoal, ref currentAction);
				}
			}

			if (currentGoal == null || (agent->InterruptionCheckCooldown <= 0 && currentGoal.IsInterruptible(currentAction) == true))
			{
				FindNewGoal(frame, context, ref currentGoal, ref currentAction);
			}

			if (currentGoal != null)
			{
				UpdateCurrentGoal(frame, context, deltaTime, ref currentGoal, ref currentAction);
			}

			Pool.Return(context);
		}

		public static void StopCurrentGoal(Frame frame, EntityRef entity)
		{
			var context = GetContext(frame, entity);

			var currentGoal = context.Agent->CurrentGoal.Id.IsValid == true ? frame.FindAsset<GOAPGoal>(context.Agent->CurrentGoal.Id) : null;
			var currentAction = GetCurrentAction(frame, context.Agent);

			StopCurrentGoal(frame, context, ref currentGoal, ref currentAction);
		}

		public static void SetGoalDisableTime(Frame frame, EntityRef entity, AssetRefGOAPGoal goal, FP disableTime)
		{
			if (goal.Id.IsValid == false)
				return;

			var agent = frame.Unsafe.GetPointer<GOAPAgent>(entity);

			if (goal == agent->CurrentGoal)
			{
				StopCurrentGoal(frame, entity);
			}

			var root = frame.FindAsset<GOAPRoot>(agent->Root.Id);
			int goalIndex = Array.IndexOf(root.GoalRefs, goal);

			if (goalIndex >= 0)
			{
				var disableTimes = frame.ResolveList(agent->GoalDisableTimes);
				disableTimes[goalIndex] = disableTime;
			}
		}

		public static void SetCustomHeuristicCost(Frame frame, GOAPHeuristic heuristicCost)
		{
			if (heuristicCost == null)
				return;

			PrepareHeuristicCost(frame, heuristicCost);
		}

		// PRIVATE METHODS

		private static void UpdateCurrentGoal(Frame frame, GOAPEntityContext context, FP deltaTime, ref GOAPGoal currentGoal, ref GOAPAction currentAction)
		{
			var agent = context.Agent;
			bool debug = DebugEntity == context.Entity;

			if (currentAction != null && agent->CurrentState.Contains(currentAction.Effects) == true)
			{
				// This action is done, let's choose another one in next step
				StopCurrentAction(frame, context, ref currentAction);
			}

			// Activate next action from the plan if needed
			if (currentAction == null && agent->CurrentPlanSize > 0)
			{
				while (agent->CurrentActionIndex < agent->CurrentPlanSize - 1)
				{
					agent->LastProcessedActionIndex = agent->CurrentActionIndex;
					agent->CurrentActionIndex++;

					var nextAction = frame.FindAsset<GOAPAction>(agent->Plan[agent->CurrentActionIndex].Id);

					if (agent->CurrentState.Contains(nextAction.Conditions) == false)
					{
						// Conditions are not met, terminate whole plan
						StopCurrentGoal(frame, context, ref currentGoal, ref currentAction);
						break;
					}

					if (agent->CurrentState.Contains(nextAction.Effects) == false)
					{
						// This action is valid, activate it
						currentAction = nextAction;
						currentAction.Activate(frame, context);

						if (debug == true)
						{
							Log.Info($"GOAP: Action {currentAction.Path} activated");
						}

						agent->CurrentActionTime = 0;
						break;
					}
				}

				if (currentAction == null && currentGoal != null)
				{
					if (debug == true)
					{
						Log.Info($"GOAP: Plan execution failed: Probably last action is finished but goal is not satisfied (state might change during execution). Goal: {currentGoal.Path}");
					}

					StopCurrentGoal(frame, context, ref currentGoal, ref currentAction);
				}
			}

			// Update action
			if (currentAction != null)
			{
				var result = currentAction.Update(frame, context);

				if (result == GOAPAction.EResult.IsFailed)
				{
					StopCurrentGoal(frame, context, ref currentGoal, ref currentAction);
				}
				else if (result == GOAPAction.EResult.IsDone)
				{
					// This action claims to be done, apply effects and next action will be chosen next Update
					agent->CurrentState.Merge(currentAction.Effects);
					agent->LastProcessedActionIndex = agent->CurrentActionIndex;

					StopCurrentAction(frame, context, ref currentAction);
				}

				agent->CurrentActionTime += deltaTime;
			}

			if (currentGoal != null)
			{
				agent->CurrentGoalTime += deltaTime;
			}
		}

		private static void StopCurrentAction(Frame frame, GOAPEntityContext context, ref GOAPAction currentAction)
		{
			if (currentAction == null)
				return;

			if (context.Agent->Plan[context.Agent->CurrentActionIndex] != currentAction)
			{
				Log.Error($"GOAP: Trying to stop action {currentAction.Path} that isn't currently active.");
				return;
			}

			currentAction.Deactivate(frame, context);
			context.Agent->LastProcessedActionIndex = context.Agent->CurrentActionIndex;

			if (context.Entity == DebugEntity)
			{
				Log.Info($"GOAP: Action {currentAction.Path} deactivated");
			}

			currentAction = null;
		}

		private static void StopCurrentGoal(Frame frame, GOAPEntityContext context, ref GOAPGoal currentGoal, ref GOAPAction currentAction)
		{
			var agent = context.Agent;

			StopCurrentAction(frame, context, ref currentAction);

			if (currentGoal != null)
			{
				currentGoal.Deactivate(frame, context);

				if (context.Entity == DebugEntity)
				{
					Log.Info($"GOAP: Goal {currentGoal.Path} deactivated");
				}

				FP disableTime = currentGoal.GetDisableTime(frame, context);
				if (disableTime > 0)
				{
					var disableTimes = frame.ResolveList(agent->GoalDisableTimes);

					int goalIndex = Array.IndexOf(context.Root.Goals, currentGoal);
					if (goalIndex >= 0)
					{
						disableTimes[goalIndex] = disableTime;
					}
				}
			}

			agent->CurrentActionIndex = -1;
			agent->LastProcessedActionIndex = -1;
			agent->CurrentActionTime = 0;

			agent->CurrentPlanSize = 0;
			agent->CurrentGoal = default;
			agent->CurrentGoalTime = 0;

			currentGoal = null;
			currentAction = null;
		}

		private static void FindNewGoal(Frame frame, GOAPEntityContext context, ref GOAPGoal currentGoal, ref GOAPAction currentAction)
		{
			var agent = context.Agent;
			var goals = context.Root.Goals;

			GOAPGoal bestGoal = null;
			FP bestRelevancy = FP.MinValue;

			var disableTimes = frame.ResolveList(agent->GoalDisableTimes);
			for (int i = 0; i < goals.Length; i++)
			{
				if (disableTimes[i] > 0)
					continue;

				var goal = goals[i];

				var startState = agent->CurrentState;
				startState.Merge(goal.StartState);

				if (startState.Contains(goal.TargetState) == true)
					continue; // Goal is satisfied

				FP relevancy = goal.GetRelevancy(frame, context);

				if (relevancy <= 0)
					continue;

				if (relevancy > bestRelevancy)
				{
					bestRelevancy = relevancy;
					bestGoal = goal;
				}
			}

			// Reset interruption timer
			agent->InterruptionCheckCooldown = context.Root.InterruptionCheckInterval;

			if (bestGoal == null || bestGoal == currentGoal)
				return;

			bool debug = context.Entity == DebugEntity;

			if (debug == true)
			{
				Log.Info($"GOAP: New best goal found: {bestGoal.Path}");
			}

			GOAPState currentState = agent->CurrentState;
			GOAPState targetState = default;

			bestGoal.InitPlanning(frame, context, ref currentState, ref targetState);

			var aStar = Pool<GOAPAStar>.Get();
			List<GOAPAction> plan = null;

			if (debug == true)
			{
				using (new StopwatchBlock("GOAP: Backward A* search"))
				{
					plan = aStar.Run(frame, context, currentState, targetState, bestGoal, context.Root.Actions, _heuristicCost, Constants.MAX_PLAN_SIZE);
				}

				Log.Info($"GOAP: Search data - {aStar.Statistics.ToString()}");
			}
			else
			{
				plan = aStar.Run(frame, context, currentState, targetState, bestGoal, context.Root.Actions, _heuristicCost, Constants.MAX_PLAN_SIZE);
			}

			if (plan == null)
			{
				if (debug == true)
				{
					Log.Info($"GOAP: Failed to find plan for goal {bestGoal.Path}");
				}

				int goalIndex = Array.IndexOf(goals, bestGoal);
				// Ensure there will be at least one planning without this failed goal
				disableTimes[goalIndex] = FPMath.Max(FP._0_50, agent->InterruptionCheckCooldown + FP._0_10);

				Pool<GOAPAStar>.Return(aStar);

				return;
			}

			if (currentGoal != null)
			{
				StopCurrentGoal(frame, context, ref currentGoal, ref currentAction);
			}

			agent->CurrentGoal = bestGoal;
			agent->CurrentGoalTime = 0;
			agent->CurrentState = currentState;
			agent->GoalState = targetState;

			agent->CurrentActionIndex = -1;
			agent->LastProcessedActionIndex = -1;
			agent->CurrentActionTime = 0;
			agent->CurrentPlanSize = 0;

			currentGoal = bestGoal;
			currentAction = null;

			for (int i = 0; i < plan.Count; i++)
			{
				var action = plan[i];
				if (action == null)
					break;

				*agent->Plan.GetPointer(i) = action;
				agent->CurrentPlanSize++;
			}

			if (debug == true)
			{
				var planInfo = $"GOAP: Plan FOUND. Size: {agent->CurrentPlanSize} More...";
				for (int i = 0; i < agent->CurrentPlanSize; i++)
				{
					planInfo += $"\nAction {i + 1}: {plan[i].Path}";
				}

				Log.Info(planInfo);
			}

			currentGoal.Activate(frame, context);

			if (debug == true)
			{
				Log.Info($"GOAP: Goal {currentGoal.Path} activated");
			}

			// Plan object is part of pooled GOAPAStar object
			// so GOAPAStar needs to be returned after plan is no longer needed
			Pool<GOAPAStar>.Return(aStar);
		}

		private static void PrepareHeuristicCost(Frame frame, GOAPHeuristic customHeuristicCost)
		{
			if (customHeuristicCost == null)
			{
				if (_heuristicCost != null)
					return; // No change

				switch (sizeof(EWorldState))
				{
					case 4:
						_heuristicCost = GOAPDefaultHeuristic.BitmaskDifferenceUInt32;
						break;
					//case 8:
					//	_heuristicCost = GOAPHeuristic.BitmaskDifferenceUInt64;
					//	break;
#pragma warning disable CS0162
					default:
						throw new NotImplementedException($"Heuristic for EWorldState size of {sizeof(EWorldState)} bytes is not implemented");
#pragma warning restore CS0162
				}
			}
			else if (_heuristicCost == null)
			{
				// Prepare custom heuristic cost for first time
				var goapData = frame.Unsafe.GetOrAddSingletonPointer<GOAPData>();
				goapData->HeuristicCost = customHeuristicCost;

				_heuristicCost = customHeuristicCost.GetHeuristicCost;
			}
			else
			{
				// Check whether custom heuristic cost is the same as existing one
				frame.Unsafe.TryGetPointerSingleton(out GOAPData* goapData);
				
				if (goapData == null || goapData->HeuristicCost != customHeuristicCost)
				{
					throw new InvalidOperationException("Heuristic cost calculation has to be same for all GOAP agents. Setting custom heuristic cost must be called before GOAPManager.Initilize method.");
				}	 
			}
		}

		private static GOAPAction GetCurrentAction(Frame frame, GOAPAgent* agent)
		{
			if (agent->CurrentActionIndex < 0)
				return null;

			if (agent->LastProcessedActionIndex >= agent->CurrentActionIndex)
				return null;

			return frame.FindAsset<GOAPAction>(agent->Plan[agent->CurrentActionIndex].Id);
		}

		private static GOAPEntityContext GetContext(Frame frame, EntityRef entity)
		{
			var context = Pool<GOAPEntityContext>.Get();

			context.Entity     = entity;
			context.Agent      = frame.Unsafe.GetPointer<GOAPAgent>(entity);
			context.Blackboard = frame.Has<AIBlackboardComponent>(entity) ? frame.Unsafe.GetPointer<AIBlackboardComponent>(entity) : null;
			context.Root       = frame.FindAsset<GOAPRoot>(context.Agent->Root.Id);
			context.Config     = frame.FindAsset<AIConfig>(context.Agent->Config.Id);

			return context;
		}
	}

	public unsafe class GOAPEntityContext
	{
		public EntityRef              Entity;
		public GOAPAgent*             Agent;
		public GOAPRoot               Root;
		public AIConfig               Config;
		public AIBlackboardComponent* Blackboard;
	}
}