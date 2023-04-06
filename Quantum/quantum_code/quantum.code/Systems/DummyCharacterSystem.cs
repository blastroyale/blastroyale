namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for <see cref="DummyCharacter"/> entity
	/// </summary>
	public class DummyCharacterSystem : SystemSignalsOnly, ISignalOnComponentAdded<DummyCharacter>, 
	                                    ISignalHealthIsZeroFromAttacker
	{
		/// <inheritdoc />
		public void HealthIsZeroFromAttacker(Frame f, EntityRef entity, EntityRef attacker, QBoolean fromRoofDamage)
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
				Team = Constants.TEAM_ID_NEUTRAL
			};
			
			f.Add(entity, new PlayerCharacter());
			
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(entity);
			playerCharacter->Player = entity.Index;
			f.GetOrAddSingleton<GameContainer>().AddPlayer(f, playerCharacter->Player, entity, 0, 0, 0, 0, -1);

			f.Add(entity, targetable);
			f.Add(entity, new Stats(component->Health, 0, 0, 0, 0, 0, 0, 0, 0, 0));

		}
	}
}