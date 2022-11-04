using System;
using System.Collections.Generic;

namespace Firstlight.Matchmaking
{
	/// <summary>
	/// Configuration required to run the matchmaking service
	/// </summary>
	public class MatchmakingConfig
	{
		/// <summary>
		/// Default lobby name to be used for the queue
		/// </summary>
		public string LobbyName { get; private set; }

		/// <summary>
		/// Queue name, configured in PlayFab
		/// </summary>
		public string QueueName { get; private set; }

		/// <summary>
		/// PubSub connection that will be used to dispatch matchmaking
		/// update sockets to clients
		/// </summary>
		public string PubSubUrl { get; private set; }

		public MatchmakingConfig(string lobby, string queue)
		{
			LobbyName = lobby;
			QueueName = queue;
			// TODO: Remove default after tests
			PubSubUrl = Environment.GetEnvironmentVariable("PUB_SUB", EnvironmentVariableTarget.Process) ??
						"Endpoint=https://flg-marketplace-dev-pubsub.webpubsub.azure.com;AccessKey=***REMOVED***;Version=1.0;";
		}
	}
}