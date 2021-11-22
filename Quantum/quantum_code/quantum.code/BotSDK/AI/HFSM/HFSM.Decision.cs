using Photon.Deterministic;
using System;

namespace Quantum {
  public abstract unsafe partial class HFSMDecision {
    public string Label;

    public abstract Boolean Decide(Frame f, EntityRef e);
  }
}
