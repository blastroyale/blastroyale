using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the actor is a bot
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class IsBotDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			return f.Has<BotCharacter>(e);
		}
	}
}