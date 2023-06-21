using System.Diagnostics;

namespace Quantum.Systems.Bots
{
	public static unsafe class BotLogger
	{
		[Conditional("BOT_DEBUG")]
		public static void LogAction(ref BotCharacterSystem.BotCharacterFilter filter, string action)
		{
			Log.Warn("Bot " + filter.BotCharacter->BotNameIndex + " " + filter.Entity + " took decision " + action);
		}

		[Conditional("BOT_DEBUG")]
		public static void LogAction(EntityRef entity, string action)
		{
			Log.Warn("Bot " + entity + " took decision " + action);
		}
	}
}