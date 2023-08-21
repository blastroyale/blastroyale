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
			playerCharacter->Player = component->PlayerReference;
			f.GetOrAddSingleton<GameContainer>().AddPlayer(f,new PlayerCharacterSetup()
			{
				playerRef = playerCharacter->Player,
				e = entity
			});

			f.Add(entity, targetable);
			f.Add(entity, new Stats(component->Health, 0, 0, 0, component->Shields, component->Shields, 0, 0, 0, 0));
			
			f.Unsafe.GetPointer<Stats>(entity)->GainShield(f, entity, component->Shields.AsInt);
		}
	}
}