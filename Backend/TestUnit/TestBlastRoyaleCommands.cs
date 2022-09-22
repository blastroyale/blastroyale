
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Backend.Game;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using NUnit.Framework;
using PlayFab;
using Photon.Deterministic;
using Quantum;
using Assert = NUnit.Framework.Assert;
using ModelSerializer = FirstLight.Server.SDK.Modules.ModelSerializer;

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
	public async Task TestNoPermissionsServiceCommands()
	{
		FeatureFlags.QUANTUM_CUSTOM_SERVER = true;
		var playerRef = new PlayerRef()
		{
			_index = 0
		};
		var matchData = new QuantumPlayerMatchData[]
		{
			new QuantumPlayerMatchData()
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
		commandData["SecretKey"] = "invalid secret key";
		var result = await _server.GetService<GameServer>().RunLogic(_server.GetTestPlayerID(), new LogicRequest()
		{
			Command = command.GetType().FullName,
			Data = commandData,
		});
		Assert.IsTrue(result?.Data["LogicException"].Contains("permission"));
	}

	[Test]
	public void TestEndOfGameCalculationsCommand()
	{
		var playerRef = new PlayerRef()
		{
			_index = 1
		};
		var matchData = new QuantumPlayerMatchData[]
		{
			new QuantumPlayerMatchData()
			{
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


}