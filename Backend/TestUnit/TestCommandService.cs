
using System.Collections.Generic;
using Backend.Game.Services;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using NUnit.Framework;
using Quantum;
using FirstLight.Server.SDK.Modules;
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
		var cmd = new EquipCollectionItemCommand() { Item = new CollectionItem(GameId.Male01Avatar) };

		var serializedCommand = ModelSerializer.Serialize(cmd).Value;
		
		Assert.AreEqual("{\"Item\":{\"Id\":\"Male01Avatar\"}}", serializedCommand);
	}
	
	[Test]
	public void TestCommandTypeFind()
	{
		var cmd = new EquipCollectionItemCommand() { Item = new CollectionItem(GameId.Male01Avatar) };
		var service = (ServerCommandHandler?)_server.GetService<IServerCommahdHandler>();

		var cmdType = service.GetCommandType(cmd.GetType().FullName);
		
		Assert.AreEqual(typeof(EquipCollectionItemCommand), cmdType);
	}
	
	[Test]
	public void TestCommandFromString()
	{
		var cmd = new EquipCollectionItemCommand() { Item = new CollectionItem(GameId.Male01Avatar) };
		var (cmdTypeName, cmdData) = ModelSerializer.Serialize(cmd);
		
		var receivedCommand = (EquipCollectionItemCommand)_server.GetService<IServerCommahdHandler>().BuildCommandInstance(cmdData, cmdTypeName);

		Assert.AreEqual(cmd.Item.Id, receivedCommand.Item.Id);
	}
}