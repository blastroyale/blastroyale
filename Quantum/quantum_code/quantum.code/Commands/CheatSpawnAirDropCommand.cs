using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// Spawns an airdrop with no delays and an optional <see cref="Position"/> and <see cref="Chest"/>.
	///
	/// If position isn't set it will spawn on top of the current player.
	/// </summary>
	public class CheatSpawnAirDropCommand : CommandBase
	{
		public bool OnPlayerPosition;
		public GameId Chest = GameId.ChestLegendary;

		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref OnPlayerPosition);

			var chest = (int) Chest;
			stream.Serialize(ref chest);
			Chest = (GameId) chest;
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var position = FPVector3.Zero;

			if (OnPlayerPosition)
			{
				var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
				position = f.Get<Transform3D>(characterEntity).Position;
			}

			AirDrop.Create(f, new QuantumShrinkingCircleConfig
			{
				AirdropStartTimeRange = new QuantumPair<FP, FP>(FP._0, FP._0),
				AirdropDropDuration = FP._10,
				AirdropChest = Chest
			}, position);
		}
	}
}