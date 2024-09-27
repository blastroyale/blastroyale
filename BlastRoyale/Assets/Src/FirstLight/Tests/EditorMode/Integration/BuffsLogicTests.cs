using System.Collections.Generic;
using BuffSystem;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using Newtonsoft.Json;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class BuffsLogicTests : IntegrationTestFixture
	{
		[Test]
		public void TestNoBuffs()
		{
			var buffs = new BuffVirtualEntity();
			
			Assert.AreEqual(0, buffs.GetStat(BuffStat.PctBonusCoins).AsInt);
		}
		
		[Test]
		public void TestAdditive()
		{
			var buffs = new BuffVirtualEntity();
			var buff = CreateBuff(BuffId.OwnsCorpo, BuffOperator.ADD, BuffStat.PctBonusCoins, 10);
			buffs.AddBuff(buff);
			
			Assert.AreEqual(10, buffs.GetStat(BuffStat.PctBonusCoins).AsInt);
			Assert.IsTrue(buffs.HasBuff(buff));
		}
		
		[Test]
		public void TestMultiplicative()
		{
			var buffs = new BuffVirtualEntity();
			FP add = 10;
			FP mult = FP._0_50;
			var buff = CreateBuff(BuffId.OwnsCorpo, BuffOperator.ADD, BuffStat.PctBonusCoins, add);
			var buff2 = CreateBuff(BuffId.OwnsPlagueDoctor, BuffOperator.MULT, BuffStat.PctBonusCoins, mult);
			buffs.AddBuff(buff);
			buffs.AddBuff(buff2);
			
			var expected = add * (1 + mult);
			Assert.AreEqual(expected, buffs.GetStat(BuffStat.PctBonusCoins));
		}
		
		[Test]
		public void TestBuffEntity()
		{
			var buffs = TestLogic.BuffsLogic.CalculateMetaEntity();
			
			Assert.AreEqual(0, buffs.GetStat(BuffStat.PctBonusCoins).AsInt);
		}
		
		private BuffConfig CreateBuff(BuffId id, BuffOperator op, BuffStat stat, FP value)
		{
			return new BuffConfig()
			{
				Id = id,
				Modifiers = new List<BuffModifierConfig>()
				{
					new () {Op = op, Stat = stat, Value = value}
				}
			};
		}
	}
}