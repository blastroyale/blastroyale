using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	public unsafe struct TopDownSystemFilter
	{
		public EntityRef Entity;
		public PlayerCharacter* Player;
		public AlivePlayerCharacter* Alive;
		public TopDownController* Controller;
	}

	public unsafe class TopDownSystem : SystemMainThreadFilter<TopDownSystemFilter>, ISignalGameEnded
	{
		public override void Update(Frame f, ref TopDownSystemFilter filter)
		{
			filter.Controller->Move(f, filter.Entity, filter.Controller->MoveDirection);
		}


		public void GameEnded(Frame f)
		{
			foreach (var livingPlayer in f.GetComponentIterator<TopDownController>())
			{
				if (f.TryGet<TopDownController>(livingPlayer.Entity, out var kcc))
				{
					kcc.MoveDirection = FPVector2.Zero;
				}
			}
		}
	}
}