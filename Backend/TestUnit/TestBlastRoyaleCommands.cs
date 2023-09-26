
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Backend.Game;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PlayFab;
using Photon.Deterministic;
using Quantum;
using Assert = NUnit.Framework.Assert;
using ModelSerializer = FirstLight.Server.SDK.Modules.ModelSerializer;
using PlayerMatchData = Quantum.PlayerMatchData;

/// <summary>
/// Test suit to test specific blast royale commands.
/// </summary>
public class TestBlastRoyaleCommands
{
	private TestServer? _server;

	[SetUp]
	public void Setup()
	{
		_server = new TestServer();
		_server.SetupInMemoryServer();
		FeatureFlags.QUANTUM_CUSTOM_SERVER = false;
	}

	[Test]
	public void TestBrCustomDictSerializer()
	{
		var data = new CollectionData();
		data.Equipped[new(GameIdGroup.PlayerSkin)] = ItemFactory.Collection (GameId.Male01Avatar);

		var serialized = ModelSerializer.Serialize(data).Value;

		var deserialized = ModelSerializer.Deserialize<CollectionData>(serialized);
		
		Assert.AreEqual(deserialized.Equipped[new(GameIdGroup.PlayerSkin)], ItemFactory.Collection(GameId.Male01Avatar));
	}

	[Test]
	public async Task TestNoPermissionsServiceCommands()
	{
		FeatureFlags.QUANTUM_CUSTOM_SERVER = true;
		var playerRef = new PlayerRef()
		{
			_index = 0
		};
		var matchData = new QuantumPlayerMatchData[]
		{
			new()
			{
				Data = new PlayerMatchData()
				{
					Player = playerRef,
					Entity = new EntityRef()
					{
						Index = 0
					},
				}
			}
		}.ToList();
		
		var command = new EndOfGameCalculationsCommand()
		{
			PlayersMatchData = matchData,
			QuantumValues = new QuantumValues()
			{
				ExecutingPlayer = playerRef,
			}
		};
		var commandData = new Dictionary<string, string>();
		commandData[CommandFields.Timestamp] = "1";
		commandData[CommandFields.ClientVersion] = "10.0.0";
		commandData[CommandFields.Command] = ModelSerializer.Serialize(command).Value;
		var request = new LogicRequest()
		{
			Command = command.GetType().FullName,
			Data = commandData,
		};
		commandData["SecretKey"] = "invalid secret key";
		Assert.ThrowsAsync<LogicException>(async () =>
		{
			await _server.GetService<GameServer>().RunLogic(_server.GetTestPlayerID(), request);
		}, "Insuficient permissions to run command");
	}

	[Test]
	public void TestEndOfGameCalculationsCommand()
	{
		var gm = _server.GetService<IConfigsProvider>().GetConfigsList<QuantumGameModeConfig>().First();
		var playerRef = new PlayerRef()
		{
			_index = 1
		};
		var matchData = new QuantumPlayerMatchData[]
		{
			new QuantumPlayerMatchData()
			{
				GameModeId = gm.Id,
				PlayerRank = 1,
				Data = new PlayerMatchData()
				{
					Player = playerRef,
					Entity = new EntityRef()
					{
						Index = 1
					},
				}
			}
		}.ToList();
		
		var command = new EndOfGameCalculationsCommand()
		{
			PlayersMatchData = matchData,
			QuantumValues = new QuantumValues()
			{
				ExecutingPlayer = playerRef,
			}
		};
		var result = _server?.SendTestCommand(command);
		Assert.IsFalse(result?.Data.ContainsKey("LogicException"));
	}

	[Test]
	public void TestFPSerializer()
	{
		var model = new QuantumPlayerMatchData()
		{
			Data = new()
			{
				LastDeathPosition = new FPVector3(1, 2, 3)
			}
		};

		var json = ModelSerializer.Serialize(model).Value;
		var model2 = ModelSerializer.Deserialize<QuantumPlayerMatchData>(json);
		
		Assert.IsFalse(json.Contains("AsInt"));
		Assert.AreEqual(model2.Data.LastDeathPosition, model.Data.LastDeathPosition);
	}
}