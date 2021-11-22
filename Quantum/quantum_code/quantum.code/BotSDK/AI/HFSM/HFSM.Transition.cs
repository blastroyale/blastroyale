using System;

namespace Quantum
{
  [Serializable]
  public class HFSMTransition
  {
    public string Id;

    public Int32 EventKey = 0;
    public AssetRefHFSMDecision DecisionLink;
    public AssetRefHFSMState StateLink;
    public AssetRefHFSMTransitionSet TransitionSetLink;

    [NonSerialized]
    public HFSMDecision Decision;
    [NonSerialized]
    public HFSMState State;
    [NonSerialized]
    public HFSMTransitionSet TransitionSet;

    public void Setup(IResourceManager resourceManager)
    {
      Decision = (HFSMDecision)resourceManager.GetAsset(DecisionLink.Id);
      State = (HFSMState)resourceManager.GetAsset(StateLink.Id);
      TransitionSet = (HFSMTransitionSet)resourceManager.GetAsset(TransitionSetLink.Id);
    }
  }
}
