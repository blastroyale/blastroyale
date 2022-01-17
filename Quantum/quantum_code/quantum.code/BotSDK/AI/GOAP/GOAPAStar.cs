using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Auto)]
	public struct GOAPNode
	{
		public int         Hash;
		public byte        Depth;
		public int         Parent;
		public sbyte       ActionIndex;
		public GOAPState   State;
		public short       F;
		public short       G;

		public string ToString(GOAPAction[] actions)
		{
			string action = ActionIndex >= 0 && ActionIndex < actions.Length ? actions[ActionIndex].Path : "NoAction";
			return $"{action}, real cost (G): {G / 100f}, total heuristic cost (F): {F / 100f}, hash: {Hash}, parent: {Parent}";
		}
	}

	public struct StateBackValidation
	{
		public GOAPState ValidatedState;
		public FP        CostToNextState;

		public StateBackValidation(GOAPState validatedState, FP costToNextState)
		{
			ValidatedState = validatedState;
			CostToNextState = costToNextState;
		}
	}

	public unsafe class GOAPAStar
	{
		public delegate int HeuristicCost(GOAPState fromState, GOAPState toState);

		// PUBLIC MEMBERS

		public StatisticsData Statistics;

		// PRIVATE MEMBERS

		private readonly Dictionary<int, GOAPNode> _active = new Dictionary<int, GOAPNode>();
		private readonly Dictionary<int, GOAPNode> _closed = new Dictionary<int, GOAPNode>();

		private readonly GOAPHeap _open = new GOAPHeap();

		private ActionData[] _actionData;
		private List<GOAPAction> _plan = new List<GOAPAction>(16);

		private List<StateBackValidation> _planStateValidations = new List<StateBackValidation>(8);

		// PUBLIC METHODS

		public List<GOAPAction> Run(Frame frame, GOAPEntityContext context, GOAPState start, GOAPState end, GOAPGoal goal,
			GOAPAction[] availableActions, HeuristicCost heuristic, int maxPlanSize)
		{
			Statistics = default;

			int foundPathEndHash = BackwardAStar(frame, context, start, end, goal, availableActions, heuristic, maxPlanSize);

			Statistics.Success     = foundPathEndHash != 0;
			Statistics.ClosedNodes = _closed.Count;
			Statistics.OpenNodes   = _open.Size;
			Statistics.ActiveNodes = _active.Count;

			if (foundPathEndHash == 0)
				return null;

			_plan.Clear();

			int nodeHash = foundPathEndHash;
			while (nodeHash != 0)
			{
				if (_closed.TryGetValue(nodeHash, out GOAPNode node))
				{
					if (node.ActionIndex >= 0)
					{
						_plan.Add(availableActions[node.ActionIndex]);
					}

					nodeHash = node.Parent;
				}
			}

			return _plan;
		}

		// PRIVATE METHODS

		private int BackwardAStar(Frame frame, GOAPEntityContext context, GOAPState start, GOAPState end, GOAPGoal goal,
			GOAPAction[] availableActions, HeuristicCost heuristic, int maxPlanSize)
		{
			_open.Clear();
			_closed.Clear();
			_active.Clear();

			PrepareActionData(ref _actionData, availableActions.Length);

			GOAPNode startNode = new GOAPNode
			{
				Hash = end.GetHashCode(),
				State = end,
				F = 0,
				G = 0,
				Depth = 0,
				ActionIndex = (sbyte)-1,
			};

			_open.Push(startNode);
			_active.Add(startNode.Hash, startNode);

			while (_open.Size > 0)
			{
				GOAPNode currentNode = _open.Pop();
				_closed.Add(currentNode.Hash, currentNode);

				//Log.Warn($"Closing node {currentNode.ToString()}");

				if (start.Contains(currentNode.State) == true)
					return currentNode.Hash;

				if (currentNode.Depth >= maxPlanSize)
					continue;

				for (int i = availableActions.Length - 1; i >= 0; i--)
				{
					var action = availableActions[i];
					var actionData = _actionData[i];

					if (actionData.IsProcessed == true && actionData.IsValid == false)
						continue;

					// Check if action can satisfy state at least partially (backward search)
					if (currentNode.State.ContainsAny(action.Effects) == false)
						continue;

					// Check if action will not incorrectly override current state (backward search)
					// Note: Next few lines are a bit difficult to understand.
					// Do not modify it unless you know what you are doing.
					// Mistake here will lead in failed backward search in more complex situations.
					// Help: Continued state says how state should look like after this action will be executed.
					// With applied state we are checking whether the current state contains continued state,
					// merge helps with checking only part of the current state that matters.
					var continuedState = GOAPState.Merge(action.Conditions, action.Effects);
					var appliedState = GOAPState.Merge(continuedState, currentNode.State);

					if (appliedState.Contains(continuedState) == false)
						continue;

					// We are validating action after we decide it fits the plan to not do this
					// potentially expensive call unnecessary
					if (actionData.IsProcessed == false)
					{
						actionData.IsValid = action.ValidateAction(frame, context, start, out FP cost) && cost > 0;
						actionData.Cost = cost;
						actionData.IsProcessed = true;

						Assert.Check(cost > 0, $"GOAP: Action cost has to be greater than zero. Action: {action.Path} Cost: {cost}");

						if (actionData.IsValid == false)
						{
							Statistics.ValidationReturns++;
							continue;
						}
					}

					Statistics.ValidationCalls++;

					// Remove effects, apply conditions to state (= backward apply action)
					GOAPState newState = GOAPState.Remove(currentNode.State, action.Effects);
					newState.Merge(action.Conditions);

					if (action.UsePlanStateValidation == true)
					{
						_planStateValidations.Clear();

						action.ValidatePlanState(frame, context, newState, currentNode.State, actionData.Cost, _planStateValidations);

						Statistics.PlanStateValidationCalls++;

						// With plan state validation the plan can branch
						for (int j = 0; j < _planStateValidations.Count; j++)
						{
							var validation = _planStateValidations[j];
							TryAddNode(currentNode, validation.ValidatedState, i, validation.CostToNextState, start, heuristic);
						}
					}
					else
					{
						TryAddNode(currentNode, newState, i, actionData.Cost, start, heuristic);
					}
				}
			}

			return 0;
		}

		private void TryAddNode(GOAPNode currentNode, GOAPState newState, int actionIndex, FP actionCost, GOAPState start, HeuristicCost heuristic)
		{
			int newStateHash = newState.GetHashCode();

			if (_closed.ContainsKey(newStateHash) == true)
			{
				Statistics.InClosedReturns++;
				return;
			}

			short h = (short)(heuristic(start, newState) * 100);
			short g = (short)(currentNode.G + actionCost * 100);

			Statistics.ProcessedNodes++;

			GOAPNode node = new GOAPNode
			{
				Hash        = newStateHash,
				State       = newState,
				ActionIndex = (sbyte)actionIndex,
				G           = g,
				F           = (short)(h + g),
				Parent      = currentNode.Hash,
				Depth       = (byte)(currentNode.Depth + 1),
			};

			if (_active.TryGetValue(node.Hash, out GOAPNode existing))
			{
				if (node.F >= existing.F)
					return;

				_open.Update(node);
				_active[node.Hash] = node;

				//Log.Warn($"Updating node {node.ToString()}");
			}
			else
			{
				_open.Push(node);
				_active.Add(node.Hash, node);

				//Log.Warn($"Adding node {node.ToString()}");
			}
		}

		private static void PrepareActionData(ref ActionData[] actionData, int length)
		{
			int originalLength = actionData != null ? actionData.Length : 0;

			if (originalLength < length)
			{
				Array.Resize(ref actionData, length);
			}

			for (int i = 0; i < length; i++)
			{
				if (i < originalLength)
				{
					actionData[i].IsProcessed = false;
				}
				else
				{
					actionData[i] = new ActionData();
				}
			}
		}

		private void PrintHeap(GOAPAction[] actions)
		{
			string heap = "HEAP: ";
			int index = 0;

			foreach (GOAPNode node in _open)
			{
				heap += $"{index}: COST: {node.F} {actions[node.ActionIndex].Path}\n";
				index++;
			}

			Log.Info(heap);
		}

		private class ActionData
		{
			public bool IsProcessed;
			public bool IsValid;
			public FP   Cost;
		}

		public struct StatisticsData
		{
			public bool Success;
			public int  ClosedNodes;
			public int  OpenNodes;
			public int  ActiveNodes;
			public int  ValidationCalls;
			public int  ValidationReturns;
			public int  PlanStateValidationCalls;
			public int  InClosedReturns;
			public int  ProcessedNodes;

			public new string ToString()
			{
				return $"Success: {Success}, Closed: {ClosedNodes}, Open: {OpenNodes}, Active: {ActiveNodes}\nValidation calls: {ValidationCalls}\nValidation returns: {ValidationReturns}\nPlan State Validation calls: {PlanStateValidationCalls}\nIn Closed returns: {InClosedReturns}\n Processed nodes: {ProcessedNodes}";
			}
		}
	}
}