using System.Diagnostics;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum.Commands
{
	/// <summary>
	/// This command tries to use a special with the SpecialIndex index
	/// </summary>
	public unsafe class SpecialUsedCommand : CommandBase
	{
		public FPVector2 AimInput;
		public int SpecialIndex;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref AimInput);
			stream.Serialize(ref SpecialIndex);
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;

			// Between sending the command and receiving it, the player might have died due to the frame delay between Unity & Quantum
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(characterEntity, out var playerCharacter))
			{
				return;
			}
			
			var special = playerCharacter->WeaponSlot->Specials[SpecialIndex];
			
			if (special.TryActivate(f, characterEntity, AimInput, SpecialIndex))
			{
				playerCharacter->WeaponSlot->Specials[SpecialIndex] = special;
			}
		}
	}
}