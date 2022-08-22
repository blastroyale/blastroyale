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
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(characterEntity);
			var special = playerCharacter->WeaponSlot->Specials[SpecialIndex];
			
			if (special.TryActivate(f, characterEntity, AimInput, SpecialIndex))
			{
				playerCharacter->WeaponSlot->Specials[SpecialIndex] = special;
			}
		}
	}
}