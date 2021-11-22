using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Collectable
	{
		/// <summary>
		/// Checks if the given <paramref name="playerRef"/> is collecting the collectable
		/// </summary>
		public bool IsCollecting(PlayerRef playerRef)
		{
			return CollectorsEndTime[playerRef] > FP._0;
		}
	}
}