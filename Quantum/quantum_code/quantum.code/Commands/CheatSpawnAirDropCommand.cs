using Photon.Deterministic;

namespace Quantum.Commands
{
	public class CheatSpawnAirDropCommand : CommandBase
	{
		public FPVector3 Position;
		public GameId Chest = GameId.ChestLegendary;

		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref Position);

			var chest = (int) Chest;
			stream.Serialize(ref chest);
			Chest = (GameId) chest;
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			if (Position == FPVector3.Zero)
			{
				var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
				Position = f.Get<Transform3D>(characterEntity).Position;
			}

			AirDrop.Create(f, new QuantumShrinkingCircleConfig
			{
				AirdropStartTimeRange = new QuantumPair<FP, FP>(FP._0, FP._0),
				AirdropDropDuration = FP._10,
				AirdropChest = Chest
			}, Position);
		}
	}
}