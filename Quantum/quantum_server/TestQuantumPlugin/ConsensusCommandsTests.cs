using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstLight.Game.Commands;
using FirstLight.Game.Utils;
using NUnit.Framework;
using Quantum;
using ServerSDK.Modules;
using Assert = NUnit.Framework.Assert;

namespace Tests
{
	public class TestCommandConsensus
	{
		private EndGameCommandConsensusHandler _consensus;
		
		[SetUp]
		public void Setup()
		{
			_consensus = new EndGameCommandConsensusHandler();
		}

		private void AddTestCommand(int playerIndex, IQuantumConsensusCommand cmd)
		{
			_consensus.ReceiveCommand(playerIndex, ConvertAndSerialize(cmd));
		}

		private EndGameConsensusCommandData ConvertAndSerialize(IQuantumConsensusCommand cmd)
		{
			var serialized = ModelSerializer.Serialize(cmd);
			var json = $"{serialized.Key}:{serialized.Value}";
			var bytes = Encoding.UTF8.GetBytes(json);
			var convertedCommand = _consensus.DeserializeCommand(bytes);
			return convertedCommand;
		}
		
		private IQuantumConsensusCommand BuildTestCommand(uint rank)
		{
			return new EndOfGameCalculationsCommand()
			{
				PlayersMatchData = new List<QuantumPlayerMatchData>()
				{
					new QuantumPlayerMatchData()
					{
						PlayerRank = rank,
						MapId = 1,
						PlayerName = rank.ToString(),
						Data = new PlayerMatchData()
						{
							DamageDone = 666
						}
					}
				}
			};
		}

		[Test]
		public void TestCommandSerialization()
		{
			var cmd1 = ConvertAndSerialize(BuildTestCommand(1));
			var cmd2 = ConvertAndSerialize(BuildTestCommand(2));
			
			Assert.AreEqual(cmd1.PlayersMatchData.First().PlayerRank, 1);
			Assert.AreEqual(cmd2.PlayersMatchData.First().PlayerRank, 2);
		}
		
		[Test]
		public void TestCommandConsensusBase()
		{
			AddTestCommand(1, BuildTestCommand(1));
			AddTestCommand(2, BuildTestCommand(1));
			AddTestCommand(3, BuildTestCommand(1));
			AddTestCommand(4, BuildTestCommand(1));

			var consensus = _consensus.GetConsensus(minPlayers:4);
			
			Assert.NotNull(consensus);
			Assert.AreEqual(4, consensus.Count);
		}
		
		[Test]
		public void TestNoConsensus()
		{
			AddTestCommand(1, BuildTestCommand(1));
			AddTestCommand(2, BuildTestCommand(2));
			AddTestCommand(3, BuildTestCommand(3));
			AddTestCommand(4, BuildTestCommand(4));

			var consensus = _consensus.GetConsensus(minPlayers:4);
			
			Assert.IsNull(consensus);
		}
		
		[Test]
		public void TestConsensusWithOneFake()
		{
			AddTestCommand(1, BuildTestCommand(1));
			AddTestCommand(2, BuildTestCommand(1));
			AddTestCommand(3, BuildTestCommand(1));
			AddTestCommand(4, BuildTestCommand(4));

			var consensus = _consensus.GetConsensus(minPlayers:3);
			
			Assert.NotNull(consensus);
			Assert.AreEqual(3, consensus.Count);
			Assert.IsTrue(consensus.ContainsKey(1));
			Assert.IsTrue(consensus.ContainsKey(2));
			Assert.IsTrue(consensus.ContainsKey(3));
			Assert.IsFalse(consensus.ContainsKey(4));
		}
	}
}

