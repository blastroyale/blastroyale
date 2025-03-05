using System;
using System.Linq;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;

namespace FirstLight.Web3.Runtime
{
	public class Web3Analytics
	{
		private static AsyncBufferedQueue _analyticsQueue = new (TimeSpan.FromSeconds(1), onlyLast:false);
		
		public static void SendEvent(string name, params (string, object)[] data)
		{
			_analyticsQueue.Add(() =>
			{
				return AsyncPlayfabAPI.WritePlaystreamAnalyticsEvent(new WriteClientPlayerEventRequest()
				{
					EventName = name,
					Body = data.ToDictionary(db => db.Item1, db => db.Item2),
				});
			});
		}
	}
}