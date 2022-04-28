using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct RaycastShots
	{
		/// <summary>
		/// Requests if this raycast shot is an instant shot or will travel over time
		/// </summary>
		public bool IsInstantShot => Speed < FP.SmallestNonZero;
	}
}