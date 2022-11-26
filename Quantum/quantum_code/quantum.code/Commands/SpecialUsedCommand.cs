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

			var aimInputProcessed = AimInput;
			var special = playerCharacter->WeaponSlot->Specials[SpecialIndex];
			
			if (aimInputProcessed.SqrMagnitude < FP.SmallestNonZero && f.TryGet<Transform3D>(characterEntity, out var transform))
			{
				aimInputProcessed = (transform.Rotation * FPVector3.Forward).XZ.Normalized * (FP._0_75 + FP._0_10);
			}
			
			if (special.TryActivate(f, characterEntity, aimInputProcessed, SpecialIndex))
			{
				playerCharacter->WeaponSlot->Specials[SpecialIndex] = special;
			}
		}
	}
}