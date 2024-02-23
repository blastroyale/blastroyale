
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using NUnit.Framework;
using Quantum;
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
		var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(GameId.Male02Avatar) };
		var receivedUser = "";

		async Task OnCommand(string userId, EquipCollectionItemCommand cmd, ServerState state)
		{
			receivedUser = userId;
		}
		
		_events.RegisterCommandListener<EquipCollectionItemCommand>(OnCommand);
		
		_events.CallCommandEvent("Yolo", cmd, new ServerState()).Wait();
		
		Assert.AreEqual("Yolo", receivedUser);
	}
}