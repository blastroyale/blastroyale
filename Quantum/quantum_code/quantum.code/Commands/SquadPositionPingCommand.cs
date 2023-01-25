using Photon.Deterministic;

namespace Quantum.Commands
{
	
	/// <summary>
	/// Send a position ping to squad members.
	/// </summary>
	public class SquadPositionPingCommand: CommandBase
	{
		public FPVector3 Position;
		public int TeamId;
		
		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref Position);
			stream.Serialize(ref TeamId);
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			f.Events.OnSquadPositionPing(characterEntity, TeamId, Position);
		}
	}

}