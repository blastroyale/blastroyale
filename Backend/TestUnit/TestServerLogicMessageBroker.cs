using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace Tests
{
	public class TestServerLogicMessageBroker
	{
		
		private TestServer _server = null!;
		private IEventManager _pluginEvents;

		[SetUp]
		public void Setup()
		{
			_server = new TestServer();
			_server.SetupInMemoryServer();
			_pluginEvents = _server.GetService<IEventManager>();
			_server.GiveDefaultSkins();
		}

		[Test]
		public void TestLogicMessagesFired()
		{
			GameLogicMessageEvent<CollectionItemEquippedMessage> receivedMessage = null;
			
			_pluginEvents.RegisterEventListener<GameLogicMessageEvent<CollectionItemEquippedMessage>>(async msg =>
			{
				receivedMessage = msg;
			});
			
			var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(GameId.FemaleAssassin) };
			
			_server.SendTestCommand(cmd);
			
			Assert.NotNull(receivedMessage);
			Assert.AreEqual(_server.GetTestPlayerID(), receivedMessage.PlayerId);
			Assert.AreEqual(GameId.FemaleAssassin, receivedMessage.Message.EquippedItem.Id);
		}
		
	}
	
}