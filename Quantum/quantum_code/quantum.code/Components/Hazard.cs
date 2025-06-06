using Photon.Deterministic;

namespace Quantum
{
	public partial struct Hazard
	{
		/// <summary>
		/// Creates an <see cref="Hazard"/> based on the given data
		/// </summary>
		public static EntityRef Create(Frame f, ref Hazard hazard, FPVector2 position)
		{
			var e = f.Create();
			var transform = new Transform2D
			{
				Position = position,
				Rotation = 0
			};

			f.Add(e, hazard);
			f.Add(e, transform);

			return e;
		}
	}
}