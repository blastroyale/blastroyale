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
			var characterEntity = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[playerRef].Entity;

			// Between sending the command and receiving it, the player might have died due to the frame delay between Unity & Quantum
			if (!f.Unsafe.TryGetPointer<PlayerInventory>(characterEntity, out var playerInventory))
			{
				return;
			}

			var aimInputProcessed = AimInput;
			var special = playerInventory->Specials[SpecialIndex];
			special.TryActivate(f, playerRef, characterEntity, aimInputProcessed, SpecialIndex);
		}
	}
}