using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// Increase player movespeed command, it works in a toggle way.
	/// </summary>
	public unsafe class CheatMoveSpeedCommand : CommandBase
	{
		public override void Serialize(BitStream stream)
		{
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG
			var entity = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[playerRef].Entity;
			var stats = f.Unsafe.GetPointer<Stats>(entity);
			uint modifierId = 6666666;
			var modifiers = f.ResolveList(stats->Modifiers);
			for (var i = 0; i < modifiers.Count; i++)
			{
				if (modifiers[i].Id == modifierId)
				{
					stats->RemoveModifier(f, entity, i);
					return;
				}
			}
			// Add

			var speedModifier = new Modifier
			{
				Id = modifierId,
				Type = StatType.Speed,
				OpType = OperationType.Multiply,
				Power = FP._5,
				Duration = FP.MaxValue,
				StartTime = FP._0,
				IsNegative = false
			};

			stats->AddModifier(f, entity, speedModifier);


#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}