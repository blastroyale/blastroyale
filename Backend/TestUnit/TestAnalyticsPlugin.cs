
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Quantum;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using Assert = NUnit.Framework.Assert;


/// <summary>
/// Test suit to test specific blast royale commands.
/// </summary>
public class TestAnalyticsPlugin
{
	private TestServer? _server;
	private InMemoryAnalytics _analytics;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_server.SetupInMemoryServer();
		_analytics = _server.GetService<IServerAnalytics>() as InMemoryAnalytics;
	}

	[Test]
	public void TestEndOfGameCalculationsCommandAnalytics()
	{
		var command = new EndOfGameCalculationsCommand()
		{
			PlayersMatchData = new List<QuantumPlayerMatchData>()
			{
				new QuantumPlayerMatchData()
				{
					MapId = 5,
					Data = new PlayerMatchData()
					{
						DamageDone = 5
					} 
				}
			}
		};
		var state = new ServerState()
		{
			{ "Key1", "Value1" }
		};
		_server.Services.GetService<IEventManager>().CallEvent(new CommandFinishedEvent("yolo", command, state, state, "{commanddata}"));
		
		Assert.That(_analytics.FiredEvents.Count == 1);
		
		var playerEvent = _analytics.FiredEvents.First();

		var oldState = playerEvent.Data["old_state"] as ServerState;
		var newState = playerEvent.Data["current_state"] as ServerState;
		Assert.IsTrue(oldState.TryGetValue("Key1", out var value) && value == "Value1");
		Assert.IsTrue(newState.TryGetValue("Key1", out var value2) && value2 == "Value1");
	}


}