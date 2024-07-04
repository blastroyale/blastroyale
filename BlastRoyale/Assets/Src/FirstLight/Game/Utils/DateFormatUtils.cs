using System;
using System.Text;

namespace FirstLight.Game.Utils
{
	public static class DateFormatUtils
	{
		public static string Display(this TimeSpan timeSpan, bool shortFormat = true, bool showDays = true, bool showHours = true, bool showMinutes = true, bool showSeconds = true)
		{
			var sb = new StringBuilder();
			var days = (int) Math.Floor(timeSpan.TotalDays);
			if (showDays && days > 0)
			{
				timeSpan -= TimeSpan.FromDays(days);
				sb.Append($"{days}{(shortFormat ? "D" : "Day" + (days == 1 ? "s" : ""))} ");
			}

			var hours = (int) Math.Floor(timeSpan.TotalHours);
			if (showHours)
			{
				timeSpan -= TimeSpan.FromHours(hours);
				sb.Append($"{hours}{(shortFormat ? "H" : "Hour" + (hours == 1 ? "s" : ""))} ");
			}

			var minutes = (int) Math.Floor(timeSpan.TotalMinutes);
			if (showMinutes)
			{
				timeSpan -= TimeSpan.FromMinutes(minutes);
				sb.Append($"{minutes}{(shortFormat ? "M" : "Minute" + (minutes == 1 ? "s" : ""))} ");
			}

			var seconds = (int) Math.Floor(timeSpan.TotalSeconds);
			if (showSeconds)
			{
				sb.Append($"{seconds}{(shortFormat ? "S" : "Second" + (minutes == 1 ? "s" : ""))} ");
			}

			return sb.ToString();
		}
	}
}