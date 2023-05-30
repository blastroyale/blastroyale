using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotRespawnBehaviour : BotBehaviour
	{
		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// If it's a deathmatch game mode and a bot is dead then we process respawn behaviour
			if (f.Context.GameModeConfig.BotRespawn && f.TryGet<DeadPlayerCharacter>(filter.Entity, out var deadBot))
			{
				// If the bot is dead and it's not yet the time to respawn then we skip the update
				if (f.Time < deadBot.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
				{
					return true;
				}

				var agent = f.Unsafe.GetPointer<HFSMAgent>(filter.Entity);
				HFSMManager.TriggerEvent(f, &agent->Data, filter.Entity, Constants.RespawnEvent);
			}

			// If a bot is not alive OR a bot is stunned 
			// then we don't go further with the behaviour
			if (!f.Has<AlivePlayerCharacter>(filter.Entity) || f.Has<Stun>(filter.Entity) ||
				// Hack to prevent the bots to act while player's camera animation is still playing
				f.Time < f.GameConfig.PlayerRespawnTime)
			{
				return true;
			}

			return false;

		}

		
	}
}