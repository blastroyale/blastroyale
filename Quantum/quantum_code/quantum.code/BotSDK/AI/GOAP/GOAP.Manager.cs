using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum {


  public enum GOAPPlannerDirection {
    Forward, Backwards
  }

  public unsafe class GOAPUpdater {

    private Planner _planner;

    public void InitGOAPUpdater(GOAPRoot goapRoot) {
      if (_planner == null)
        _planner = new Planner(goapRoot);
    }

    public Boolean UpdateGOAPAgent(Frame f, GOAPAgent* goap, EntityRef e) {
      GOAPPlannerDirection direction = GOAPPlannerDirection.Forward;

      if (_planner == null) {
        Log.Info("The GOAP planner is not initialized");
        return false;
      }

      // copy current GOAP state data (should be kept updated from ouside)
      var current = goap;

      // check for goal state achieved first (this will also be used in planning)
      if (_planner.SatisfiesGoal(current->CurrentState, goap->Goal))
        return true;

      // re-plan if needed
      //goap->CurrentTaskIndex = -1; // force re-plan
      if (goap->CurrentTaskIndex == -1) {
        goap->TaskCount = 0;

        _planner.Search(current->CurrentState, current->Goal, direction, false);
        while (_planner.Plan.Count > 0 && goap->TaskCount < 4 && goap->TaskCount < _planner.Plan.Count) {
          // filling in the refs
          Int32 index = goap->TaskCount;
          if (direction == GOAPPlannerDirection.Forward)
            index = _planner.Plan.Count - 1 - goap->TaskCount;

          *goap->Plan.GetPointer(goap->TaskCount) = _planner.Plan[index];

          goap->TaskCount++;
          goap->CurrentTaskIndex = 0;
        }
      }

      // update current action (change live data)
      if (goap->CurrentTaskIndex == -1)
        return false;

      GOAPTask a = f.FindAsset<GOAPTask>(goap->Plan.GetPointer(goap->CurrentTaskIndex)->Id);

      if (a != null)
      {
        a.Update(f, e);
      }
      return false;
    }

    public class Planner {

      private List<GOAPTask> _tasks = new List<GOAPTask>();
      public List<GOAPTask> Plan = new List<GOAPTask>();

      private GOAPHeap _open = new GOAPHeap();
      private Dictionary<Int64, GOAPNode> _active = new Dictionary<Int64, GOAPNode>();
      private Dictionary<Int64, GOAPNode> _closed = new Dictionary<Int64, GOAPNode>();

      public Boolean SatisfiesGoal(Int64 state, GOAPState goal) {
        return (state & goal.Positive) == goal.Positive && ((~state & goal.Negative) == goal.Negative);
      }

      public void AddConditions(ref Int64 state, Int64 conditions) {
        state |= conditions;
      }

      public Planner(GOAPRoot goapRoot) {
        GOAPRoot root = goapRoot;

        foreach (var task in root.Tasks) {
          _tasks.Add(task);
        }
      }

      public Planner(IEnumerable<GOAPTask> tasks) {
        _tasks.AddRange(tasks);
      }

      public void FillPath(Int64 pathKey, GOAPPlannerDirection direction, Boolean debug) {
        var key = pathKey;

        while (key != default(Int64)) {
          if (debug)
            Log.Info(key);
          GOAPNode node = default(GOAPNode);
          if (_closed.TryGetValue(key, out node)) {
            if (node.Action != null)
              Plan.Add(node.Action);
            key = node.ParentKey;
          } else {
            // no path
          }
        }

        if (!debug)
          return;

        if (direction == GOAPPlannerDirection.Backwards) {
          for (Int32 i = 0; i < Plan.Count; i++) {
            Log.Info("Task[" + i + "]: " + Plan[i].Label);
          }
        } else {
          for (Int32 i = Plan.Count - 1; i >= 0; i--) {
            Int32 pos = Plan.Count - 1 - i;
            Log.Info("Task[" + pos + "]: " + Plan[i].Label);
          }
        }
      }

      public void DebugNode(GOAPNode node, string extra) {
        Log.Info("Debugging for " + extra);
        if (node.Action != null)
          Log.Info("A: " + node.Action.GetType().Name + ", F: " + node.F + ", G: " + node.G + ", H: " + node.H);
        Log.Info("State: (" + node.State + "), Goal: (" + node.Goal + ")");
      }

      private Byte BitflagHeuristic(Int64 state1, Int64 state2) {
        // TODO negative
        var combined = state1 & state2;
        Byte bitsNotSet = 0;
        for (Int32 i = 1; i <= 3; i++) {
          Int64 value = (Int64)(1 << i);
          if ((value & combined) == default(Int64)) {
            bitsNotSet++;
          }
        }
        return bitsNotSet;
      }

      private void BackwardAStar(Int64 state, GOAPState goal, out Int64 pathKey, Boolean debug = false) {
        //_open.Clear();
        //_closed.Clear();
        //_active.Clear();

        //GOAPNode goalNode = new GOAPNode()
        //{
        //  Goal = goal,
        //  State = state,
        //  F = 0,
        //  G = 0,
        //  H = 0
        //};
        //_open.Push(ref goalNode);
        //_active.Add(goalNode.Goal, goalNode);

        //while (_open.Size > 0)
        //{
        //  GOAPNode currentNode = _open.Pop();
        //  _closed.Add(currentNode.Goal, currentNode);

        //  if (debug)
        //  {
        //    Log.Info("");
        //    DebugNode(currentNode, "visiting");
        //  }


        //  if (SatisfiesGoal(currentNode.State, currentNode.Goal))
        //  {
        //    // found path
        //    if (debug)
        //      Log.Info("Found: " + currentNode.Action);
        //    pathKey = GOAPWorldState.Root;
        //    _closed.Add(pathKey, currentNode);
        //    return;
        //  } else
        //  {
        //    foreach (GOAPTask a in _tasks)
        //    {
        //      if (debug)
        //        Log.Info("checking " + a.GetType().Name);
        //      // generate node consequence state, and new goal state
        //      GOAPWorldState newGoal = (currentNode.Goal | a.Conditions) & ~a.Consequences & ~currentNode.State;
        //      if (_closed.ContainsKey(newGoal))
        //      {
        //        continue;
        //      }

        //      GOAPWorldState newState = currentNode.State | a.Consequences;
        //      // actiond oesn't add anything
        //      if (newState == currentNode.State)
        //        continue;

        //      Int32 H = BitflagHeuristic(newState, state);
        //      Int32 G = currentNode.G + BitflagHeuristic(currentNode.State, newState);
        //      GOAPNode node = new GOAPNode()
        //      {
        //        Goal = newGoal,
        //        State = newState,
        //        Action = a,
        //        H = H,
        //        G = G,
        //        F = H + G,
        //        ParentKey = currentNode.Goal
        //      };
        //      if (debug)
        //        DebugNode(node, "neighbor");
        //      GOAPNode aux = default(GOAPNode);
        //      if (_active.TryGetValue(newGoal, out aux))
        //      {
        //        if (G > aux.G)
        //          continue;
        //        _open.Update(ref node);
        //        _active[newGoal] = node;
        //      } else
        //      {
        //        _open.Push(ref node);
        //        _active.Add(newGoal, node);
        //      }
        //    }
        //  }
        //}
        // no path
        pathKey = default(Int64);
      }

      private void ForwardAStar(Int64 state, GOAPState goal, out Int64 pathKey, Boolean debug = false) {
        _open.Clear();
        _closed.Clear();
        _active.Clear();

        GOAPNode goalNode = new GOAPNode() {
          Goal = goal,
          State = state,
          F = 0,
          G = 0,
          H = 0
        };
        _open.Push(ref goalNode);
        _active.Add(goalNode.State, goalNode);

        while (_open.Size > 0) {
          GOAPNode currentNode = _open.Pop();
          _closed.Add(currentNode.State, currentNode);

          if (debug) {
            Log.Info("");
            DebugNode(currentNode, "visiting");
          }

          if (SatisfiesGoal(currentNode.State, currentNode.Goal)) {
            // found path
            if (debug)
              Log.Info("Found: " + currentNode.Action);
            pathKey = 1;
            _closed.Add(pathKey, currentNode);
            return;
          } else {
            foreach (GOAPTask a in _tasks) {
              if (debug)
                Log.Info("checking " + a.GetType().Name);
              // generate node consequence state, and new goal state

              // pre-conditions not met
              var hasNegative = (a.Conditions.Negative & ~currentNode.State) == a.Conditions.Negative;
              var hasConditions = (a.Conditions.Positive & currentNode.State) == a.Conditions.Positive;
              if (!hasConditions || !hasNegative)
                continue;

              Int64 newState = (currentNode.State | a.Consequences.Positive) & ~a.Consequences.Negative;
              // actiond doesn't add anything or state already visited
              if (newState == currentNode.State || _closed.ContainsKey(newState))
                continue;

              Int32 H = BitflagHeuristic(newState, goal.Positive);
              Int32 G = currentNode.G + BitflagHeuristic(currentNode.State, newState);
              GOAPNode node = new GOAPNode() {
                Goal = currentNode.Goal,
                State = newState,
                Action = a,
                H = H,
                G = G,
                F = H + G,
                ParentKey = currentNode.State
              };
              if (debug)
                DebugNode(node, "neighbor");
              GOAPNode aux = default(GOAPNode);
              if (_active.TryGetValue(newState, out aux)) {
                if (G > aux.G)
                  continue;
                _open.Update(ref node);
                _active[newState] = node;
              } else {
                _open.Push(ref node);
                _active.Add(newState, node);
              }
            }
          }
        }
        // no path
        pathKey = default(Int64);
      }

      public void Search(Int64 current, GOAPState goal, GOAPPlannerDirection direction = GOAPPlannerDirection.Backwards, Boolean debug = false) {
        Plan.Clear();
        Int64 pathRoot;
        if (direction == GOAPPlannerDirection.Backwards)
          BackwardAStar(current, goal, out pathRoot, debug);
        else
          ForwardAStar(current, goal, out pathRoot, debug);
        if (debug) {
          Log.Info("");
          Log.Info("PATH:");
        }
        FillPath(pathRoot, direction, debug);
      }



    }


  }
}
