using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command is invoked when a player sends an emoji
	/// </summary>
	public class PlayerEmojiCommand : CommandBase
	{
		// TODO: convert Emojis to a seperate EmojiId or move it completely to the In-Game chat system
		public GameId Emoji;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			var emoji = (int) Emoji;
			
			stream.Serialize(ref emoji);

			Emoji = (GameId) emoji;
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			
			f.Events.OnPlayerEmojiSent(playerRef, characterEntity, Emoji);
		}
	}
}