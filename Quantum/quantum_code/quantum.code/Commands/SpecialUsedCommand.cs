using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum.Commands
{
	/// <summary>
	/// This cheat command destroys entity references of DumbAi components in the simulation.
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
			
			if (QuantumHelpers.IsDestroyed(f, characterEntity) || !f.Has<Targetable>(characterEntity) || 
			    !f.Unsafe.TryGetPointer<Weapon>(characterEntity, out var weapon))
			{
				return;
			}

			var special = weapon->Specials.GetPointer(SpecialIndex);
			if (!special->IsValid || !special->IsSpecialAvailable(f))
			{
				return;
			}
			
			if (special->TryUse(f, characterEntity, AimInput))
			{
				special->HandleUsed(f, characterEntity, playerRef);
			}
		}
	}
}