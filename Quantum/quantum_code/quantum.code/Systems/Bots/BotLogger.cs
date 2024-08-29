using System;
using System.Diagnostics;

namespace Quantum.Systems.Bots
{
	public static unsafe class BotLogger
	{
		[Conditional("BOT_DEBUG")]
		public static void LogAction(ref BotCharacterSystem.BotCharacterFilter filter, string action)
		{
			Log.Warn($"[{DateTime.Now.Ticks}] Bot " + filter.BotCharacter->BotNameIndex + " " + filter.Entity + " took decision " + action);
		}
		
		[Conditional("BOT_DEBUG")]
		public static void LogAction(EntityRef entity, string action)
		{
			Log.Warn($"[{DateTime.Now.Ticks}] Bot " + entity + " took decision " + action);
		}
		
		[Conditional("BOT_DEBUG")]
		public static void LogAction(in BotCharacter bot, string action)
		{
			Log.Warn($"[{DateTime.Now.Ticks}] Bot " + bot.BotNameIndex + " took decision " + action);
		}
	}
}