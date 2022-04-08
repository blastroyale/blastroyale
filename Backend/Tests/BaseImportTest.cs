using Backend.Game;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using NUnit.Framework;


public class Tests
{
	[Test]
	public void TestGameLogicImported()
	{
		var cfg = new ConfigsProvider();
		cfg.AddSingletonConfig(new MapConfig());
		var broker = new MessageBrokerService();
		var data = new ServerData();
		var analytics = new AnalyticsService();
		var audio = new TestServerAudio();
		var logic = new GameLogic(broker, null, data, analytics, cfg, audio);
		logic.Init();
		
		Assert.That(logic.EquipmentLogic.Inventory.Count == 0);
	}
}