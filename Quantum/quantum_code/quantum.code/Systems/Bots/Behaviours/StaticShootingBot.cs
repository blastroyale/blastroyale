using System;
using Photon.Deterministic;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class StaticShootingBot
	{

		internal void Update(Frame f, ref BotCharacterFilter filter)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			var player = f.Unsafe.GetPointer<PlayerCharacter>(filter.Entity);
			if (bb->GetBoolean(f, Constants.IsAimPressedKey))
			{
				var stats = f.Unsafe.GetPointer<Stats>(filter.Entity);
				stats->SetCurrentAmmo(f, player, filter.Entity, FP._1);
				return;
			}

			var weaponConfig = f.WeaponConfigs.GetConfig(player->CurrentWeapon.GameId);
			PlayerCharacterSystem.OnStartAiming(f, bb, weaponConfig, player, filter.Entity);
			bb->Set(f, Constants.IsAimPressedKey, true);
		}
	}
}