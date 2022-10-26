namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for <see cref="DummyCharacter"/> entity
	/// </summary>
	public class DummyCharacterSystem : SystemSignalsOnly, ISignalOnComponentAdded<DummyCharacter>, 
	                                    ISignalHealthIsZeroFromAttacker
	{
		/// <inheritdoc />
		public void HealthIsZeroFromAttacker(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (f.Has<DummyCharacter>(entity))
			{
				f.Add<EntityDestroyer>(entity);
				
				f.Events.OnDummyCharacterKilled(entity);
			}
		}

		public unsafe void OnAdded(Frame f, EntityRef entity, DummyCharacter* component)
		{
			var targetable = new Targetable
			{
				Team = (int) TeamType.Neutral
			};
			
			f.Add(entity, new PlayerCharacter());
			
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(entity);
			playerCharacter->Player = entity.Index;
			//f.Unsafe.GetPointerSingleton<GameContainer>()->AddPlayer(f, playerCharacter->Player, entity, 0, 0, 0, 0);

			f.Add(entity, targetable);
			f.Add(entity, new Stats(component->Health, 0, 0, 0, 0, 0, 0, 0, 0));
		}
	}
}