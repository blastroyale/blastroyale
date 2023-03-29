using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class RngLogicTest : MockedTestFixture<RngData>
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
			Assert.AreEqual(2028190331, _rngLogic.Peek);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void Peek_Twice_SameResult()
		{
			Assert.AreEqual(2028190331, _rngLogic.Peek);
			Assert.AreEqual(2028190331, _rngLogic.Peek);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void NextCheck()
		{
			Assert.AreEqual(2028190331, _rngLogic.Next);
			Assert.AreEqual(1, TestData.Count);
		}

		[Test]
		public void Next_Twice_NewResult()
		{
			Assert.AreEqual(2028190331, _rngLogic.Next);
			Assert.AreEqual(978780922, _rngLogic.Next);
			Assert.AreEqual(2, TestData.Count);
		}

		[Test]
		public void NextFpCheck()
		{
			Assert.AreEqual(0.9444427d, (double) _rngLogic.NextFp, 0.000001);
			Assert.AreEqual(1, TestData.Count);
		}

		[Test]
		public void NextFp_Twice_NewResult()
		{
			Assert.AreEqual(0.9444427d, (double) _rngLogic.NextFp, 0.000001);
			Assert.AreEqual(0.45578d, (double) _rngLogic.NextFp, 0.000001);
			Assert.AreEqual(2, TestData.Count);
		}

		[Test]
		public void PeekFp_Twice_SameResult()
		{
			Assert.AreEqual(0.9444427d, (double) _rngLogic.PeekFp, 0.000001);
			Assert.AreEqual(0.9444427d, (double) _rngLogic.PeekFp, 0.000001);
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekRangeCheck()
		{
			Assert.AreEqual(0, _rngLogic.PeekRange(0, 1));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekRange_Twice_SameResult()
		{
			Assert.AreEqual(0, _rngLogic.PeekRange(0, 1));
			Assert.AreEqual(0, _rngLogic.PeekRange(0, 1));
			Assert.AreEqual(0, TestData.Count);
		}

		[Test]
		public void PeekRange_MaxNotBigger_TrowsException()
		{
			Assert.Throws<IndexOutOfRangeException>(() => _rngLogic.PeekRange(2, 1));
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