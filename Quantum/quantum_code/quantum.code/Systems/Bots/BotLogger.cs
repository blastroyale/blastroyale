using System;
using System.Diagnostics;

namespace Quantum.Systems.Bots
{
	public static unsafe class BotLogger
	{
		[Conditional("BOT_DEBUG")]
		public static void LogAction(Frame f, ref BotCharacterSystem.BotCharacterFilter filter, string action, TraceLevel lvl = TraceLevel.Info)
		{
			LogAction(f, filter.Entity, action, lvl);
		}

		[Conditional("BOT_DEBUG")]
		public static void LogAction(Frame f, EntityRef entity, string action, TraceLevel lvl = TraceLevel.Info)
		{
			if (lvl == TraceLevel.Warning || lvl == TraceLevel.Error)
			{
				Log.Warn("BOTDEBUG: " + entity + " - " + action);
			}

			f.Events.BotDebugInfo(entity, action, (byte)lvl);
		}
	}
}