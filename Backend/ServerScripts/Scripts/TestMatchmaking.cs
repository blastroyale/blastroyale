using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using PlayFab.MultiplayerModels;
using Scripts.Base;
using static TestMatchmaking.MMStatus;
using EntityKey = PlayFab.MultiplayerModels.EntityKey;


public class TestMatchmaking : PlayfabScript
{
	private Dictionary<string, CreateMatchmakingTicketResult> _tickets = new();

	public override void Execute(ScriptParameters args)
	{
		TestFetchingConfigFromPlayfab().Wait();
	}

	public enum MMStatus
	{
		Pending,
		Matched,
		Timeout,
		Error,
		NotStarted
	}

	public static DateTime Started;

	public static void Log(string text)
	{
		var seconds = DateTime.UtcNow.Subtract(Started).TotalSeconds;

		Console.WriteLine($"{seconds.ToString("##.#")}s: " + text);
	}

	private class Player
	{
		public static async Task<Player> Create(string displayName, int delay)
		{
			var p = new Player(displayName);
			p.DelaySeconds = delay;
			await p.CreatePlayfabUser();

			return p;
		}

		public string Id;
		public string DisplayName;
		public LoginResult LoginResult;
		public CreateMatchmakingTicketResult Ticket;
		public PlayFabAuthenticationContext AuthCtx;
		public string Queue;
		public MMStatus LastStatus = NotStarted;
		public GetMatchmakingTicketResult LastTicket = null;
		public int DelaySeconds;

		private Player(string displayName)
		{
			this.DisplayName = displayName;
		}

		private async Task CreatePlayfabUser()
		{
			Id = "randombullshitblablah" + DisplayName;
			var response = await PlayFabClientAPI.LoginWithCustomIDAsync(new()
			{
				CustomId = Id,
				CreateAccount = true
			}).HandleError();
			AuthCtx = response.AuthenticationContext;
			LoginResult = response;
		}

		public async Task JoinMatchmaking(string queue, int timeout = 30)
		{
			Queue = queue;
			var mmResponse = await PlayFabMultiplayerAPI.CreateMatchmakingTicketAsync(new()
			{
				AuthenticationContext = AuthCtx,
				MembersToMatchWith = null,
				QueueName = queue,
				GiveUpAfterSeconds = timeout,
				Creator = new MatchmakingPlayer()
				{
					Entity = new EntityKey()
					{
						Id = LoginResult.AuthenticationContext.EntityId,
						Type = LoginResult.AuthenticationContext.EntityType
					},
					Attributes = new CustomMatchmakingPlayerProperties()
					{
						Server = "test",
						MasterPlayerId = LoginResult.PlayFabId
					}.Encode()
				}
			}).HandleError();
			Log($"{DisplayName} joined mm!");

			LastStatus = Pending;
			Ticket = mmResponse;
		}

		public async Task<GetMatchResult> GetMatch()
		{
			return await PlayFabMultiplayerAPI.GetMatchAsync(new GetMatchRequest()
			{
				AuthenticationContext = AuthCtx,
				QueueName = Queue,
				MatchId = LastTicket.MatchId,
				ReturnMemberAttributes = true,
			}).HandleError();
		}

		public async Task<Tuple<MMStatus, GetMatchmakingTicketResult>> FetchTicket()
		{
			if (LastStatus is not Pending)
			{
				return new Tuple<MMStatus, GetMatchmakingTicketResult>(LastStatus, LastTicket);
			}

			var ticketInfo = await PlayFabMultiplayerAPI.GetMatchmakingTicketAsync(new()
			{
				QueueName = Queue,
				TicketId = Ticket.TicketId,
				AuthenticationContext = AuthCtx
			}).HandleError();

			MMStatus status = Pending;

			if (ticketInfo.Status == "Matched")
			{
				status = Matched;
			}
			else if (ticketInfo.Status == "Canceled")
			{
				if (ticketInfo.CancellationReasonString == "Timeout")
				{
					status = Timeout;
				}
				else
				{
					status = Error;
				}
			}

			LastStatus = status;
			LastTicket = ticketInfo;
			if (status != Pending)
			{
				Log($"{DisplayName} got status " + status + " MatchId:" + ticketInfo.MatchId);
			}

			return new Tuple<MMStatus, GetMatchmakingTicketResult>(status, ticketInfo);
		}

		public async Task LateJoin(string queue, int timeout)
		{
			await Task.Delay(DelaySeconds * 1000);
			await JoinMatchmaking(queue, timeout);
		}
	}


	private async Task Run(Player[] players, string queue, int ticketTimeout, uint teamSize = 1)
	{
		bool EveryOneFinished()
		{
			return players.All(a => a.LastStatus != Pending && a.LastStatus != NotStarted);
		}

		Started = DateTime.UtcNow;
		// Join queue
		foreach (var player in players)
		{
			player.LateJoin(queue, ticketTimeout);
		}

		Log($"Simulation started!");

		while (!EveryOneFinished())
		{
			var tasks = players.Select(p => p.FetchTicket());
			await Task.WhenAll(tasks);
			await Task.Delay(7000);
		}

		Log($"Simulation finished!");
		Console.WriteLine("");
		Console.WriteLine("");
		var matches = new Dictionary<string, GetMatchResult>();
		var teamDistribution = new Dictionary<string, Dictionary<string, string>>();
		// Fetch matches

		foreach (var p in players)
		{
			if (!string.IsNullOrEmpty(p.LastTicket.MatchId) && !matches.ContainsKey(p.LastTicket.MatchId))
			{
				var match = await p.GetMatch();
				var membersWithTeam = match.Members
					.ToDictionary(player => player.Entity.Id,
						player => player.TeamId
					);


				// This distribution should be deterministic and used in the server to validate if anyone is exploiting
				membersWithTeam = TeamDistribution.Distribute(membersWithTeam, teamSize);
				matches[p.LastTicket.MatchId] = match;
				teamDistribution[p.LastTicket.MatchId] = membersWithTeam;
			}
		}

		var timedOut = players.Where(p => p.LastStatus == Timeout || p.LastStatus == Error).Select(p => p.DisplayName).ToList();
		if (timedOut.Count > 0)
		{
			Console.WriteLine("Timed out players: " + string.Join(",", timedOut));
		}

		var matched = players.Where(p => p.LastStatus == Matched).ToList();

		var matchesWithPlayer = matched.GroupBy(pl => pl.LastTicket.MatchId).ToDictionary(gdc => gdc.Key, gdc => gdc.ToList());
		foreach (var (match, matchPlayers) in matchesWithPlayer)
		{
			Console.WriteLine("");
			Console.WriteLine("Match ID: " + match + " Total Players " + matches[match].Members.Count);
			Console.WriteLine(" Teams:");
			var teams = matchPlayers.GroupBy(p => teamDistribution[p.LastTicket.MatchId][p.AuthCtx.EntityId])
				.ToDictionary(gdc => gdc.Key, gdc => gdc.ToList());
			foreach (var (team, teamPlayers) in teams)
			{
				Console.WriteLine(" - " + team + ": " + string.Join(",", teamPlayers.Select(p => p.DisplayName)));
			}
		}

		/*foreach (var (_a, m) in matches)
		{
			Console.WriteLine(ModelSerializer.PrettySerialize(m));
		}*/
	}

	public async Task TestFetchingConfigFromPlayfab()
	{
		var simulatedPlayerAmount = 15;
		var additionalWaitPerPlayer = 1;

		var queue = "battleroyale_1";
		var timeout = 15;
		uint queueTeamSize = 1;
		
		/*
		var queue = "battleroyale_2";
		var timeout = 25;
		uint queueTeamSize = 2*/;
		// Quads
		/*var queue = "battleroyale_4";
		var timeout = 40;
		uint queueTeamSize = 4;*/

		var players = new Player[simulatedPlayerAmount];
		Console.WriteLine("Creating players!");
		for (int x = 0; x < simulatedPlayerAmount; x++)
		{
			players[x] = await Player.Create("p" + x, x * additionalWaitPerPlayer);
			await Task.Delay(3000);
		}

		try
		{
			await Run(players, queue, timeout, queueTeamSize);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}


	class CustomMatchmakingPlayerProperties
	{
		public string MasterPlayerId;
		public string Server;
		public int PlayerCount = 1;

		public MatchmakingPlayerAttributes Encode()
		{
			return new MatchmakingPlayerAttributes()
			{
				EscapedDataObject = PlayFabSimpleJson.SerializeObject(this)
			};
		}

		public static CustomMatchmakingPlayerProperties Decode(MatchmakingPlayerAttributes attributes)
		{
			return PlayFabSimpleJson.DeserializeObject<CustomMatchmakingPlayerProperties>(attributes.DataObject.ToString());
		}
	}

	public override Environment GetEnvironment()
	{
		return Environment.DEV;
	}
}