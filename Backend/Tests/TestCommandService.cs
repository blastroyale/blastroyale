
using Tests.Stubs;
using FirstLight.Game.Commands;
using NUnit.Framework;
using Quantum;

namespace Tests;

public class TestCommandManager
{
	private TestServer? _server;
	
	[SetUp]
	public void SetUp()
	{
		_server = new TestServer();
	}
	
	[Test]
	public void TestCommandSerialization()
	{
		var cmd = new UpdatePlayerSkinCommand()
		{
			SkinId = GameId.Male01Avatar
		};

		var serializedCommand = _server.CommandService.SerializeCommandToServer(cmd);
		
		Assert.AreEqual("{\"SkinId\":\"Male01Avatar\"}", serializedCommand);
	}
}