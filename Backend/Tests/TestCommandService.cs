
using System.Collections.Generic;
using Backend.Game.Services;
using Tests.Stubs;
using FirstLight.Game.Commands;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace Tests;

public class TestCommandManager
{
	private TestServer? _server;
	private GameCommandService? _service;
	
	[SetUp]
	public void SetUp()
	{
		_server = new TestServer();
		_service = (GameCommandService?)_server.GetService<IGameCommandService>();
	}
	
	[Test]
	public void TestCommandSerialization()
	{
		var cmd = new UpdatePlayerSkinCommand()
		{
			SkinId = GameId.Male01Avatar
		};

		var serializedCommand = ModelSerializer.Serialize(cmd).Value;
		
		Assert.AreEqual("{\"SkinId\":\"Male01Avatar\"}", serializedCommand);
	}
	
	[Test]
	public void TestCommandTypeFind()
	{
		var cmd = new UpdatePlayerSkinCommand();
		var service = (ServerCommandHandler?)_server.GetService<IServerCommahdHandler>();

		var cmdType = service.GetCommandType(cmd.GetType().FullName);
		
		Assert.AreEqual(typeof(UpdatePlayerSkinCommand), cmdType);
	}
	
	[Test]
	public void TestCommandFromArgs()
	{
		var sentCommand = new UpdatePlayerSkinCommand()
		{
			SkinId = GameId.Barrel // a skin to look like a barrel !! $_$
		};
		var args = new Dictionary<string, string>();
		var (cmdTypeName, cmdData) = ModelSerializer.Serialize(sentCommand);
		args[CommandFields.Command] = cmdData;
		
		var receivedCommand = (UpdatePlayerSkinCommand)_server.GetService<IServerCommahdHandler>().BuildCommandInstance(args, cmdTypeName);

		Assert.AreEqual(sentCommand.SkinId, receivedCommand.SkinId);
	}

	[Test]
	public void TestQuantumCommand()
	{
		var command = new GameCompleteRewardsCommand()
		{
			PlayerMatchData = new QuantumPlayerMatchData()
			{
				Data = new PlayerMatchData()
				{
					Player = new PlayerRef()
					{
						_index = 1234
					},
					Entity = new EntityRef()
				}
			}
		};
		var args = new Dictionary<string, string>();
		var (cmdTypeName, cmdData) = ModelSerializer.Serialize(command);
		args[CommandFields.Command] = cmdData;
		
		var receivedCommand = (GameCompleteRewardsCommand)_server.GetService<IServerCommahdHandler>().BuildCommandInstance(args, cmdTypeName);
		
		Assert.AreEqual(command.PlayerMatchData.Data.Player._index, receivedCommand.PlayerMatchData.Data.Player._index);
		
	}
}