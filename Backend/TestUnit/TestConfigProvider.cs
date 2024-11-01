
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using NUnit.Framework;
using ServerCommon.CommonServices;

public class TestConfigsProvider
{
	private TestServer _server = null!;
	private string _playerId = null!;
	private PlayerData _playerData;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_server.SetupInMemoryServer();
		_playerId = _server.GetTestPlayerID();
	}
	
	/// <summary>
	/// TestUnit the current data in server is serializable as configuration.
	/// </summary>
	[Test]
	public void TestCurrentDevData()
	{
		var cfg = new EmbeddedConfigProvider();
		foreach (var cfgType in cfg.GetAllConfigs().Keys)
		{
			Assert.NotNull(cfg.GetConfigByType(cfgType));
		}
	}

	[Test]
	public void TestUpdatingConfigs()
	{
		var config = new ConfigsProvider();
		var oldValue = new Dictionary<Type, IEnumerable>()
		{
			{
				typeof(PlayerLevelConfig), new Dictionary<int, PlayerLevelConfig>()
				{
					{0, new PlayerLevelConfig()
					{
						LevelUpXP = 10
					}}
				}
			}
		};

		var newValue = new Dictionary<Type, IEnumerable>()
		{
			{
				typeof(PlayerLevelConfig), new Dictionary<int, PlayerLevelConfig>()
				{
					{0, new PlayerLevelConfig()
					{
						LevelUpXP = 50
					}}
				}
			}
		};
		
		config.AddAllConfigs(oldValue);

		var cfg = config.GetConfigsDictionary<PlayerLevelConfig>().Values.First();
		Assert.AreEqual(0, config.Version);
		Assert.AreEqual(10, cfg.LevelUpXP);

		config.UpdateTo(1, newValue);
		
		cfg = config.GetConfigsDictionary<PlayerLevelConfig>().Values.First();
		Assert.AreEqual(1, config.Version);
		Assert.AreEqual(50, cfg.LevelUpXP);
	}
}