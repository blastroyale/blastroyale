using Photon.Deterministic;

namespace Quantum
{
	public partial struct Hazard
	{
		/// <summary>
		/// Creates an <see cref="Hazard"/> based on the given <paramref name="config"/>
		/// </summary>
		public static EntityRef Create(Frame f, QuantumHazardConfig config, FPVector3 position, EntityRef source,
		                                      int teamSource)
		{
			var hazard = new Hazard
			{
				Attacker = source,
				EndTime = f.Time + config.Lifetime,
				GameId = config.Id,
				Interval = config.Interval,
				NextTickTime = f.Time + config.InitialDelay,
				PowerAmount = config.PowerAmount,
				Radius = config.Radius,
				StunDuration = FP._0,
				TeamSource = teamSource
			};
			
			return Create(f, hazard, position);
		}
		
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