
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Quantum;
using ServerSDK;
using ServerSDK.Events;
using ServerSDK.Models;
using Tests.Stubs;
using Assert = NUnit.Framework.Assert;

namespace Tests;

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
		_server.Services.GetService<IEventManager>().CallEvent(new CommandFinishedEvent("yolo", command, state, "{commanddata}"));
		
		Assert.That(_analytics.FiredEvents.Count == 2);

		var commandEvent = _analytics.FiredEvents.First();
		var playerEvent = _analytics.FiredEvents.Last();

		Assert.AreEqual("{commanddata}", commandEvent.Data["CommandData"]);
		Assert.IsTrue(playerEvent.Data.TryGetValue("Key1", out var value) && value == "Value1");
	}


}