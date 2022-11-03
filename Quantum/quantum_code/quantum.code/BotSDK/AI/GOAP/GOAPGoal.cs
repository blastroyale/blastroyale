using Photon.Deterministic;

namespace Quantum
{
	public abstract unsafe partial class GOAPGoal
	{
		public string                Label;

		[BotSDKHidden]
		public GOAPState             StartState;
		[BotSDKHidden]
		public GOAPState TargetState;
	}
}