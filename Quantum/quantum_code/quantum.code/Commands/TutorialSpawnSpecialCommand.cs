using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// Used during tutorial to enable the special grenade spawner after killing the bots
	/// </summary>
	public unsafe class TutorialSpawnSpecialCommand : CommandBase
	{
		public override void Serialize(BitStream stream)
		{
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			if (!f.Context.IsTutorial)
			{
				return;
			}

			foreach (var entityComponentPair in f.Unsafe.GetComponentBlockIterator<CollectablePlatformSpawner>())
			{
				if (entityComponentPair.Component->Disabled)
				{
					entityComponentPair.Component->Disabled = false;
				}
			}
		}
	}
}