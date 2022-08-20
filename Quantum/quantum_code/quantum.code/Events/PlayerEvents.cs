using System.Collections.Generic;

namespace Quantum
{
	public unsafe partial class EventOnLocalPlayerDead
	{
		public QuantumPlayerMatchData PlayerData;
	}
	
	public unsafe partial class EventOnPlayerKilledPlayer
	{
		public List<QuantumPlayerMatchData> PlayersMatchData;
	}
	
	public unsafe partial class EventOnPlayerDead
	{
		public bool IsSuicide => Entity == EntityKiller;
	}
	
	public partial class Frame 
	{
		public unsafe partial struct FrameEvents 
		{
			public void OnPlayerWeaponChanged(PlayerRef player, EntityRef entity, int slot)
			{
				var playerCharacter = _f.Unsafe.GetPointer<PlayerCharacter>(entity);
				var ev = OnPlayerWeaponChanged(player, entity, playerCharacter->CurrentWeapon, slot);

				if (ev == null)
				{
					return;
				}

				OnLocalPlayerWeaponChanged(player, entity, *playerCharacter->WeaponSlot, slot);
			}
			
			public void OnLocalPlayerDead(PlayerRef player, PlayerRef killer, EntityRef killerEntity)
			{
				var data = _f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData;
				var matchData = data[player];
				
				var ev = OnLocalPlayerDead(player, matchData.Entity, killer, killerEntity);

				if (ev == null)
				{
					return;
				}

				ev.PlayerData = new QuantumPlayerMatchData(_f, matchData);
			}
			
			public void OnPlayerSpecialUsed(EntityRef entity, Special special, int specialIndex)
			{
				var playerCharacter = _f.Unsafe.GetPointer<PlayerCharacter>(entity);
				var ev = OnPlayerSpecialUsed(playerCharacter->Player, entity, special, specialIndex);

				if (ev == null || !_f.Context.IsLocalPlayer(playerCharacter->Player))
				{
					return;
				}

				OnLocalPlayerSpecialUsed(playerCharacter->Player, entity, special, specialIndex);
			}
			
			public void OnPlayerDamaged(EntityRef entity, Spell spell, int maxHealth, int previousShield, 
			                            int maxShield, int previousHealth, uint healthDamage)
			{
				if (!_f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var playerCharacter))
				{
					return;
				}
				
				var shieldDamage = spell.PowerAmount - healthDamage;
				
				OnPlayerDamaged(playerCharacter->Player, entity, spell.Attacker, spell.PowerAmount, shieldDamage, 
				                previousShield, maxShield, healthDamage, previousHealth, maxHealth,  spell.OriginalHitPosition);
			}
			
			public void OnPlayerKilledPlayer(PlayerRef playerDead, PlayerRef playerKiller)
			{
				var container = _f.Unsafe.GetPointerSingleton<GameContainer>();
				var data = container->PlayersData;
				var matchData = container->GetPlayersMatchData(_f, out var leader);
				var ev = OnPlayerKilledPlayer(playerDead, data[playerDead].Entity, 
				                              playerKiller, data[playerKiller].Entity, 
				                              leader, data[leader].Entity);

				if (ev == null)
				{
					return;
				}
				
				ev.PlayersMatchData = matchData;
			}
		}
	}
}