using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="NavMeshSteeringAgent"/> movement callbacks
	/// It handles the <see cref="NavMeshPathfinder"/> update movement behaviour
	/// </summary>
	public unsafe class NavMeshAgentSystem : SystemSignalsOnly
	                                         //,ISignalOnNavMeshMoveAgent // TODO: Use when Quantum 2.1 is released so we can move agents with height
	{

		/// <inheritdoc />
		public void OnNavMeshMoveAgent(Frame f, EntityRef entity, FPVector2 desiredDirection)
		{
			var transform = f.Unsafe.GetPointer<Transform3D>(entity);
			
			transform->Position.X.RawValue = transform->Position.X.RawValue + ((desiredDirection.X.RawValue * f.DeltaTime.RawValue) >> FPLut.PRECISION);
			transform->Position.Z.RawValue = transform->Position.Z.RawValue + ((desiredDirection.Y.RawValue * f.DeltaTime.RawValue) >> FPLut.PRECISION);
			
			QuantumHelpers.LookAt2d(transform, desiredDirection);
			
			// TODO: Do the Y position calculation when Quantum 2.1 is released
		}
	}
}