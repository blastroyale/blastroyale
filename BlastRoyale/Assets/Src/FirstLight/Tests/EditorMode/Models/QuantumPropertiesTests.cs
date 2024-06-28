using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services.RoomService;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Models
{
	public class QuantumPropertiesTests
	{
		[Test]
		public void TestPropertiesHashConversion()
		{
			var room1 = new RoomProperties();
			var metaItemDropOverwrite = new MetaItemDropOverwrite()
			{
				Id = GameId.COIN,
				Place = DropPlace.Chest,
				DropRate = FP._0_05,
				MaxDropAmount = 1,
				MinDropAmount = 1,
			};
			room1.SimulationMatchConfig.Value = new SimulationMatchConfig()
			{
				MatchType = MatchType.Matchmaking,
				Mutators = new[] {"mutator"},
				MetaItemDropOverwrites = new[]
				{
					metaItemDropOverwrite
				},
				GameModeID = "thisisagamemode"
			};
			room1.Commit.Value = "commityolo";

			var table1 = room1.ToHashTable();

			var room2 = new RoomProperties();
			room2.FromHashTable(table1);

			Assert.AreEqual("commityolo", room2.Commit.Value);
			Assert.AreEqual("thisisagamemode", room2.SimulationMatchConfig.Value.GameModeID);
			Assert.AreEqual(MatchType.Matchmaking, room2.SimulationMatchConfig.Value.MatchType);
			Assert.Contains("mutator", room2.SimulationMatchConfig.Value.Mutators);
			var overwrites = room2.SimulationMatchConfig.Value.MetaItemDropOverwrites;
			Assert.AreEqual(1, overwrites.Length);
			Assert.AreEqual(overwrites[0], metaItemDropOverwrite);
			Assert.Contains("mutator", room2.SimulationMatchConfig.Value.Mutators);
		}

		[Test]
		public void TestPropertiesSystemHash()
		{
			var hash = new Hashtable();
			hash.Add("started", true);

			var room = new RoomProperties();
			room.FromSystemHashTable(hash);

			Assert.IsTrue(room.GameStarted.Value);
		}
	}
}