using System;
using Photon.Deterministic;
using static Quantum.Systems.Bots.BotCharacterSystem;

namespace Quantum.Systems.Bots
{
	public unsafe class StaticShootingBot
	{

		internal void Update(Frame f, ref BotCharacterFilter filter, in BotUpdateGlobalContext botCtx)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(filter.Entity);
			var stats = f.Unsafe.GetPointer<Stats>(filter.Entity);
			var player = f.Unsafe.GetPointer<PlayerCharacter>(filter.Entity);
			if (bb->GetBoolean(f, Constants.IsAimPressedKey))
			{
				stats->SetCurrentAmmo(f, player, filter.Entity, FP._1);
				return;
			}

			var weaponConfig = f.WeaponConfigs.GetConfig(player->CurrentWeapon.GameId);
			PlayerCharacterSystem.OnStartAiming(f, bb, weaponConfig);
			bb->Set(f, Constants.IsAimPressedKey, true);
		}
	}
}