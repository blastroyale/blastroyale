using System;
using Photon.Deterministic;

namespace Quantum
{

  public abstract unsafe partial class AIAction
  {
    public string Label;
    public const int NEXT_ACTION_DEFAULT = -1;

    public abstract void Update(Frame f, EntityRef e);
    public virtual int NextAction(Frame f, EntityRef e) { return NEXT_ACTION_DEFAULT; }
  }
}
