using Photon.Deterministic;

namespace Quantum
{
	public partial struct Hazard
	{
		/// <summary>
		/// Creates an <see cref="Hazard"/> based on the given data
		/// </summary>
		public static EntityRef Create(Frame f, Hazard hazard, FPVector3 position)
		{
			var e = f.Create();
			var transform = new Transform3D
			{
				Position = position,
				Rotation = FPQuaternion.Identity
			};

			f.Add(e, hazard);
			f.Add(e, transform);

			return e;
		}
	}
}