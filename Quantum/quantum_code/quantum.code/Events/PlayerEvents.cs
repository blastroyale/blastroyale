using System;
using System.Collections.Generic;
using Photon.Deterministic;

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

	public unsafe partial class EventOnPlayerDamaged
	{
		public EntityRef Entity => Spell.Victim;
		public EntityRef Attacker => Spell.Attacker;
		public UInt32 TotalUnblockedDamage => Spell.PowerAmount;
		public FPVector3 HitPosition => Spell.OriginalHitPosition;
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

			public void OnPlayerSpecialUsed(EntityRef entity, Special special, int specialIndex, FPVector2 aimInput, FP maxRange)
			{
				var playerCharacter = _f.Unsafe.GetPointer<PlayerCharacter>(entity);
				var attackerPosition = _f.Unsafe.GetPointer<Transform3D>(entity)->Position;
				var hitPosition = attackerPosition + (FPVector2.ClampMagnitude(aimInput, FP._1) * maxRange).XOY;
				
				var ev = OnPlayerSpecialUsed(playerCharacter->Player, entity, special, specialIndex, hitPosition);

				if (ev == null || !_f.Context.IsLocalPlayer(playerCharacter->Player))
				{
					return;
				}

				OnLocalPlayerSpecialUsed(playerCharacter->Player, entity, special, specialIndex, hitPosition);
			}

			public void OnPlayerDamaged(Spell spell, int totalDamage, int shieldDamageAmount, int healthDamageAmount,
			                            int previousHealth, int maxHealth, int previousShield, int maxShield)
			{
				if (!_f.Unsafe.TryGetPointer<PlayerCharacter>(spell.Victim, out var playerCharacter))
				{
					return;
				}

				OnPlayerDamaged(playerCharacter->Player, spell, (uint) totalDamage, (uint) shieldDamageAmount,
				                previousShield, maxShield, (uint) healthDamageAmount, previousHealth, maxHealth);
			}

			public void OnPlayerKilledPlayer(PlayerRef playerDead, PlayerRef playerKiller)
			{
				var container = _f.Unsafe.GetPointerSingleton<GameContainer>();
				var data = container->PlayersData;
				var matchData = container->GetPlayersMatchData(_f, out var leader);
				var ev = OnPlayerKilledPlayer(playerDead, data[playerDead].Entity,
				                              playerKiller, data[playerKiller].Entity,
				                              leader, data[leader].Entity, data[playerKiller].CurrentKillStreak,
				                              data[playerKiller].CurrentMultiKill);

				if (ev == null)
				{
					return;
				}

				ev.PlayersMatchData = matchData;
			}
		}
	}
}