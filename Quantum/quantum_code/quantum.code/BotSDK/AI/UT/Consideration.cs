using Photon.Deterministic;
using System;
using Quantum.Prototypes;

namespace Quantum
{
  [Serializable]
  public struct ResponseCurvePack
  {
    public FP MultiplyFactor;
    public AssetRefAIFunctionFP ResponseCurveRef;
    [NonSerialized] public ResponseCurve ResponseCurve;
  }

  public unsafe partial class Consideration
  {
    public string Label;

    public AssetRefAIFunctionInt RankRef;
    public AssetRefAIFunctionBool CommitmentRef;
    public AssetRefConsideration[] NextConsiderationsRefs;
    public AssetRefAIAction[] OnEnterActionsRefs;
    public AssetRefAIAction[] OnUpdateActionsRefs;
    public AssetRefAIAction[] OnExitActionsRefs;

    [NonSerialized] public AIFunctionInt Rank;
    [NonSerialized] public AIFunctionBool Commitment;
    [NonSerialized] public Consideration[] NextConsiderations;
    [NonSerialized] public AIAction[] OnEnterActions;
    [NonSerialized] public AIAction[] OnUpdateActions;
    [NonSerialized] public AIAction[] OnExitActions;

    public ResponseCurvePack[] ResponseCurvePacks;

    public FP BaseScore;

    public UTMomentumData MomentumData;
    public FP Cooldown;

    public byte Depth;

    public int GetRank(Frame frame, EntityRef entity = default)
    {
      if (Rank == null)
        return 0;

      return Rank.Execute(frame, entity);
    }

    public FP Score(Frame frame, EntityRef entity = default)
    {
      if (ResponseCurvePacks.Length == 0)
        return 0;

      FP score = 1;
      for (int i = 0; i < ResponseCurvePacks.Length; i++)
      {
        score *= ResponseCurvePacks[i].ResponseCurve.Execute(frame, entity) * ResponseCurvePacks[i].MultiplyFactor;

        // If we find a negative veto, the final score would be zero anyways, so we stop here
        if(score == 0)
        {
          break;
        }
      }

      score += BaseScore;

      FP modificationFactor = 1 - (1 / ResponseCurvePacks.Length);
      FP makeUpValue = (1 - score) * modificationFactor;
      FP finalScore = score + (makeUpValue * score);

      return finalScore;
    }

    public void OnEnter(Frame frame, UtilityReasoner* reasoner, EntityRef entity = default)
    {
      for (int i = 0; i < OnEnterActions.Length; i++)
      {
        OnEnterActions[i].Update(frame, entity);
      }
    }

    public void OnExit(Frame frame, UtilityReasoner* reasoner, EntityRef entity = default)
    {
      for (int i = 0; i < OnExitActions.Length; i++)
      {
        OnExitActions[i].Update(frame, entity);
      }
    }

    public void OnUpdate(Frame frame, UtilityReasoner* reasoner, EntityRef entity = default)
    {
      for (int i = 0; i < OnUpdateActions.Length; i++)
      {
        OnUpdateActions[i].Update(frame, entity);
      }

      if(NextConsiderationsRefs != null && NextConsiderationsRefs.Length > 0)
      {
        Consideration chosenConsideration = reasoner->SelectBestConsideration(frame, NextConsiderations, (byte)(Depth + 1), reasoner, entity);
        if(chosenConsideration != default)
        {
          chosenConsideration.OnUpdate(frame, reasoner, entity);
          UTManager.ConsiderationChosen?.Invoke(entity, chosenConsideration.Identifier.Guid.Value);
        }
      }
    }

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      Rank = (AIFunctionInt)resourceManager.GetAsset(RankRef.Id);

      if (ResponseCurvePacks != null)
      {
        for (Int32 i = 0; i < ResponseCurvePacks.Length; i++)
        {
          ResponseCurvePacks[i].ResponseCurve = (ResponseCurve)resourceManager.GetAsset(ResponseCurvePacks[i].ResponseCurveRef.Id);
          ResponseCurvePacks[i].MultiplyFactor = 1;
        }
      }

      OnEnterActions = new AIAction[OnEnterActionsRefs == null ? 0 : OnEnterActionsRefs.Length];
      if (OnEnterActionsRefs != null)
      {
        for (Int32 i = 0; i < OnEnterActionsRefs.Length; i++)
        {
          OnEnterActions[i] = (AIAction)resourceManager.GetAsset(OnEnterActionsRefs[i].Id);
        }
      }

      OnUpdateActions = new AIAction[OnUpdateActionsRefs == null ? 0 : OnUpdateActionsRefs.Length];
      if (OnEnterActionsRefs != null)
      {
        for (Int32 i = 0; i < OnUpdateActionsRefs.Length; i++)
        {
          OnUpdateActions[i] = (AIAction)resourceManager.GetAsset(OnUpdateActionsRefs[i].Id);
        }
      }

      OnExitActions = new AIAction[OnExitActionsRefs == null ? 0 : OnExitActionsRefs.Length];
      if (OnEnterActionsRefs != null)
      {
        for (Int32 i = 0; i < OnExitActionsRefs.Length; i++)
        {
          OnExitActions[i] = (AIAction)resourceManager.GetAsset(OnExitActionsRefs[i].Id);
        }
      }

      Commitment = (AIFunctionBool)resourceManager.GetAsset(CommitmentRef.Id);

      NextConsiderations = new Consideration[NextConsiderationsRefs == null ? 0 : NextConsiderationsRefs.Length];
      if (NextConsiderationsRefs != null)
      {
        for (Int32 i = 0; i < NextConsiderationsRefs.Length; i++)
        {
          NextConsiderations[i] = (Consideration)resourceManager.GetAsset(NextConsiderationsRefs[i].Id);
        }
      }
    }
  }
}
