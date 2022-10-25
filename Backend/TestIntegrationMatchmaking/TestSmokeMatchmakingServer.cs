using System.Collections.Generic;
using FirstLight.Server.SDK.Modules;
using NUnit.Framework;
using PlayFab;
using PlayFab.MultiplayerModels;

namespace TestUnitMatchmaking
{
	public class MatchmakingSmokeTest
	{
		private TestMatchmakingServer _app;
		private PlayFabAuthenticationContext _auth;
		
		[OneTimeSetUp]
		public void Setup()
		{
			_app = new TestMatchmakingServer();
		}

		[Test]
		public void SmokeTest()
		{
			Dictionary<string, string> data;
			
			_app.Post("/matchmaking/LeaveMatchmaking?key=devkey", _app.BuildUserRequest());
			data = _app.Post("/matchmaking/StartMatchmaking?key=devkey", _app.BuildUserRequest()).Result.Data;
			if (!data.TryGetValue("ticket", out var ticketId))
			{
				Assert.Fail("Could not obtain ticket from matchmaking");
			}

			var getTicketRequest = _app.BuildUserRequest();
			getTicketRequest.FunctionArgument.Data["ticket"] = ticketId;
			data = _app.Post("/matchmaking/GetTicket?key=devkey", getTicketRequest).Result.Data;
			var ticketData = ModelSerializer.DeserializeFromData<GetMatchmakingTicketResult>(data);
			Assert.AreEqual(ticketId, ticketData.TicketId);
			
			data = _app.Post("/matchmaking/GetTickets?key=devkey", _app.BuildUserRequest()).Result.Data;
			var ticketList = ModelSerializer.DeserializeFromData<ListMatchmakingTicketsForPlayerResult>(data);
			Assert.True(ticketList.TicketIds.Contains(ticketId));
			
			_app.Post("/matchmaking/LeaveMatchmaking?key=devkey", _app.BuildUserRequest());
			data = _app.Post("/matchmaking/GetTickets?key=devkey", getTicketRequest).Result.Data;
			ticketList = ModelSerializer.DeserializeFromData<ListMatchmakingTicketsForPlayerResult>(data);
			Assert.False(ticketList.TicketIds.Contains(ticketId));
		}
	}
}

