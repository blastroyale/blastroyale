
using FirstLight.Game.Commands;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Tests.Stubs;
using Assert = NUnit.Framework.Assert;

namespace Tests;

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
	}

	[Test]
	public void TestEndOfGameCalculationsCommand()
	{
		var playerRef = new PlayerRef()
		{
			_index = 0
		};
		var command = new EndOfGameCalculationsCommand()
		{
			DidPlayerQuit = false,
			LocalPlayerRef = playerRef,
			PlayedRankedMatch = true,
			PlayersMatchData = new ()
			{
				new QuantumPlayerMatchData()
				{
					Data = new PlayerMatchData()
					{
						Player = playerRef,
						LastDeathPosition = new FPVector3(2,3,4),
						Entity = new EntityRef()
						{
							Index = 0
						},
					}
				}
			}
		};
		var result = _server?.SendTestCommand(command);
		Assert.IsFalse(result?.Data.ContainsKey("LogicException"));
	}


}