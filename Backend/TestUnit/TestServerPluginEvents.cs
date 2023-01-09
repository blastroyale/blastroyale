
using System;
using Backend.Game;
using Backend.Game.Services;
using Backend.Plugins;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Quantum;
using FirstLight.Server.SDK.Services;
using FirstLight.Services;
using Assert = NUnit.Framework.Assert;

public class TestServerPluginEvents
{
	private TestServer _server = null!;
	private IEventManager _events;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_server.SetupInMemoryServer();
		_events = _server.GetService<IEventManager>()!;
	}

	[Test]
	public void TestCommandEvents()
	{
		var command = new UpdatePlayerSkinCommand();
		var receivedUser = "";

		void OnCommand(string userId, UpdatePlayerSkinCommand cmd, ServerState state)
		{
			receivedUser = userId;
		}
		
		_events.RegisterCommandListener<UpdatePlayerSkinCommand>(OnCommand);
		
		_events.CallCommandEvent("Yolo", command, new ServerState());
		
		Assert.AreEqual("Yolo", receivedUser);
	}
}