using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// Send a position ping to squad members.
	/// </summary>
	public unsafe class TeamPositionPingCommand : CommandBase
	{
		public FPVector2 Position;
		public TeamPingType Type;

		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref Position);

			var type = (int) Type;
			stream.Serialize(ref type);
			Type = (TeamPingType) type;
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[playerRef].Entity;
			var teamId = f.Unsafe.GetPointer<Targetable>(characterEntity)->Team;
			f.Events.OnTeamPositionPing(characterEntity, teamId, Position, Type);
		}
	}
}