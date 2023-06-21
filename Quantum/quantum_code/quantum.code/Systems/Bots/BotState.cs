using Photon.Deterministic;
using Quantum.Systems;
using Quantum.Systems.Bots;

namespace Quantum
{
	/// <summary>
	/// Centralized place for bot state changing functions
	/// All functions in this file should be GETTERS or SETTERS
	/// </summary>
	public unsafe static class BotState
	{
		/// <summary>
		/// Sets the bot attack target.
		/// Will set the needed keys on bots BB component and rotate the bot towards the target.
		/// Will also set the "Target" property of the bot.
		/// </summary>
		public static void SetAttackTarget(this BotCharacterSystem.BotCharacterFilter botFilter, Frame f,  EntityRef target)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botFilter.Entity);
			var player = f.Unsafe.GetPointer<PlayerCharacter>(botFilter.Entity);
			var weaponConfig = f.WeaponConfigs.GetConfig(player->CurrentWeapon.GameId);
			bb->Set(f, Constants.IsAimPressedKey, true);
			QuantumHelpers.LookAt2d(f, botFilter.Entity, target, FP._0);
			if (botFilter.BotCharacter->Target != target)
			{
				bb->Set(f, nameof(Constants.NextTapTime), f.Time + (weaponConfig.IsMeleeWeapon ? 0 : PlayerCharacterSystem.AIM_DELAY));
			}
			botFilter.BotCharacter->Target = target;
		}
	}
}