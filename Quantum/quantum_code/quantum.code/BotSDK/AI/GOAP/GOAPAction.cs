using Photon.Deterministic;
using System.Collections.Generic;

namespace Quantum
{
	public abstract unsafe partial class GOAPAction
	{
		public string    Label;

		[BotSDKHidden]
		public GOAPState Conditions;
		[BotSDKHidden]
		public GOAPState Effects;
	}
}