using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services.RoomService;
using NUnit.Framework;
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
			room1.MatchType.Value = MatchType.Matchmaking;
			room1.Commit.Value = "commityolo";
			room1.AllowedRewards.Value = new List<GameId>() { GameId.Avatar1 };
			
			var table1 = room1.ToHashTable();

			var room2 = new RoomProperties();
			room2.FromHashTable(table1);
			
			Assert.IsTrue(room2.AllowedRewards.Value.Contains(GameId.Avatar1));
			Assert.AreEqual("commityolo", room2.Commit.Value);
			Assert.AreEqual(MatchType.Matchmaking, room2.MatchType.Value);
		}
		
		[Test]
		public void TestPropertiesSystemHash()
		{
			var hash = new Hashtable();
			hash.Add("alrewards", "XP,CS,BPP,Trophies");

			var room = new RoomProperties();
			room.FromSystemHashTable(hash);

			Assert.IsTrue(room.AllowedRewards.Value.Contains(GameId.XP));
			Assert.IsTrue(room.AllowedRewards.Value.Contains(GameId.CS));
			Assert.IsTrue(room.AllowedRewards.Value.Contains(GameId.BPP));
			Assert.IsTrue(room.AllowedRewards.Value.Contains(GameId.Trophies));
		}
	}
}