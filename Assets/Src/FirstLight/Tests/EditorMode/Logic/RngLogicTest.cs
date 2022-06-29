using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using NUnit.Framework;
using UnityEngine;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class RngLogicTest : BaseTestFixture<RngData>
	{
		private const int _seed = 1;
		private RngLogic _rngLogic;

		[SetUp]
		public void Init()
		{
			_rngLogic = new RngLogic(GameLogic, DataService);

			SetupData(_seed);
		}

		[Test]
		public void PeekCheck()
		{
			Assert.AreEqual(2028190336, _rngLogic.Peek);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void Peek_Twice_SameResult()
		{
			Assert.AreEqual(2028190336, _rngLogic.Peek);
			Assert.AreEqual(2028190336, _rngLogic.Peek);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekFpCheck()
		{
			Assert.AreEqual(3.21379567E+38f, _rngLogic.PeekFp);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekFp_Twice_SameResult()
		{
			Assert.AreEqual(3.21379567E+38f, _rngLogic.PeekFp);
			Assert.AreEqual(3.21379567E+38f, _rngLogic.PeekFp);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void NextCheck()
		{
			Assert.AreEqual(2028190336, _rngLogic.Next);
			Assert.AreEqual(1, TestData.Count);
		}

		[Test]
		public void Next_Twice_NewResult()
		{
			Assert.AreEqual(2028190336, _rngLogic.Next);
			Assert.AreEqual(978780928, _rngLogic.Next);
			Assert.AreEqual(2, TestData.Count);
		}

		[Test]
		public void NextFpCheck()
		{
			Assert.AreEqual(3.21379567E+38f, _rngLogic.NextFp);
			Assert.AreEqual(1, TestData.Count);
		}

		[Test]
		public void NextFp_Twice_NewResult()
		{
			Assert.AreEqual(3.21379567E+38f, _rngLogic.NextFp);
			Assert.AreEqual(1.55094019E+38f, _rngLogic.NextFp);
			Assert.AreEqual(2, TestData.Count);
		}

		[Test]
		public void PeekRangeCheck()
		{
			Assert.AreEqual(0, _rngLogic.PeekRange(0,1));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekRange_Twice_SameResult()
		{
			Assert.AreEqual(0, _rngLogic.PeekRange(0,1));
			Assert.AreEqual(0, _rngLogic.PeekRange(0,1));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekRange_MaxInclusive_Check()
		{
			SetupData(2);
			Assert.AreEqual(1, _rngLogic.PeekRange(0,1, true));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekRange_MaxNotBigger_TrowsException()
		{
			Assert.Throws<IndexOutOfRangeException>(() => _rngLogic.PeekRange(2, 1));
			Assert.Throws<IndexOutOfRangeException>(() => _rngLogic.PeekRange(1, 1, true));
		}

		[Test]
		public void PeekFpRangeCheck()
		{
			Assert.AreEqual(0.944449723f, _rngLogic.PeekRange(0,1f));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekFpRange_Twice_SameResult()
		{
			Assert.AreEqual(0.944449723f, _rngLogic.PeekRange(0,1f));
			Assert.AreEqual(0.944449723f, _rngLogic.PeekRange(0,1f));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekFpRange_MaxInclusive_Check()
		{
			var state = new int[TestData.State.Length];
			state[0] = 0;
			state[1] = int.MaxValue;
			state[1 + 21] = 0;
			
			TestData = new RngData { Count = 0, Seed = _seed, State = state };
			
			Assert.AreEqual(1f, _rngLogic.PeekRange(0,1f));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekFpRange_MaxNotBigger_TrowsException()
		{
			Assert.Throws<IndexOutOfRangeException>(() => _rngLogic.PeekRange(2, 1f));
			Assert.Throws<IndexOutOfRangeException>(() => _rngLogic.PeekRange(1, 1f));
		}
		

		[Test]
		public void RestoreCheck()
		{
			var state = RngUtils.GenerateRngState(TestData.Seed);
			var count = 10;

			for (var i = 0; i < count; i++)
			{
				RngUtils.Next(state);
			}
			
			_rngLogic.Restore(count);
			
			CollectionAssert.AreEqual(state, TestData.State, new IntComparer());
			Assert.AreEqual(count, TestData.Count);

			state = RngUtils.GenerateRngState(TestData.Seed);
			count = 5;

			for (var i = 0; i < count; i++)
			{
				RngUtils.Next(state);
			}
			
			_rngLogic.Restore(count);
			
			CollectionAssert.AreEqual(state, TestData.State, new IntComparer());
			Assert.AreEqual(count, TestData.Count);
			
			
		}

		private void SetupData(int seed)
		{
			TestData = new RngData
			{
				Count = 0,
				Seed = seed,
				State = RngUtils.GenerateRngState(seed)
			};
		}
		
		private class IntComparer : IComparer, IComparer<int>
		{
			public int Compare(int x, int y)
			{
				return x - y;
			}

			public int Compare(object x, object y)
			{
				return Compare((int) x, (int) y);
			}
		}
	}
}