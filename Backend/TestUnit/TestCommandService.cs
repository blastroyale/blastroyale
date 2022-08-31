
using System.Collections.Generic;
using Backend.Game.Services;
using FirstLight.Game.Commands;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using NUnit.Framework;
using Quantum;
using ServerSDK.Modules;
using Assert = NUnit.Framework.Assert;

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
	public void TestCommandFromString()
	{
		var sentCommand = new UpdatePlayerSkinCommand()
		{
			SkinId = GameId.Barrel // a skin to look like a barrel !! $_$
		};
		var (cmdTypeName, cmdData) = ModelSerializer.Serialize(sentCommand);
		
		var receivedCommand = (UpdatePlayerSkinCommand)_server.GetService<IServerCommahdHandler>().BuildCommandInstance(cmdData, cmdTypeName);

		Assert.AreEqual(sentCommand.SkinId, receivedCommand.SkinId);
	}
}