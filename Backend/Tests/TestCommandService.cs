
using Tests.Stubs;
using FirstLight.Game.Commands;
using FirstLight.Game.Services;
using NUnit.Framework;
using Quantum;

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

		var serializedCommand = _service?.SerializeCommandToServer(cmd);
		
		Assert.AreEqual("{\"SkinId\":\"Male01Avatar\"}", serializedCommand);
	}
}