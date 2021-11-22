using System;
using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// This decision waits for specified amount of time before returns true
    /// </summary>
    [Serializable]
    [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
    public partial class TimerDecisionDecision : HFSMDecision
    {
        public AIBlackboardValueKey TimeToTrueState;

        /// <inheritdoc />
        public override unsafe bool Decide(Frame f, EntityRef e)
        {
            var bbComponent = f.Get<AIBlackboardComponent>(e);
            var requiredTime = bbComponent.GetFP(f, TimeToTrueState.Key);
            return f.Get<HFSMAgent>(e).Data.Time >= requiredTime;
        }
    }
}
