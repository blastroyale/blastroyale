using System;
using System.Collections;
using System.IO;
using FirstLight.Game.Presenters;
using FirstLight.Game.TestCases;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FirstLight.Tests.PlayTests
{
	[RequiresPlayMode, Category("PlayMode")]
	public class SmokeTests : ITestPlayerBuildModifier
	{
		private static IEnumerator Fail(string reason)
		{
			Debug.LogError(reason);
			yield return new ExitPlayMode();
		}

		[OneTimeSetUp]
		public void Setup()
		{
		}


		[UnityTest]
		[Timeout(1000 * 60 * 10)]
		public IEnumerator TwoMatches()
		{
			return RunTestCase(new PlayMatch(2));
		}

		[UnityTest]
		[Timeout(1000 * 60 * 20)]
		public IEnumerator TenMatches()
		{
			yield return RunTestCase(new PlayMatch(10));
		}

		[UnityTest]
		[Timeout(1000 * 60 * 60 * 12)]
		public IEnumerator HundredMatches()
		{
			yield return RunTestCase(new PlayMatch(100, false));
		}


		private IEnumerator RunTestCase(PlayTestCase instance)
		{
			MainInstaller.Clean();
			var runner = FLGTestRunner.Instance;
			runner.FailInstruction = Fail;
			yield return runner.RunInUnitTest(instance);
			yield return new ExitPlayMode();
		}

		public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
		{
			playerOptions.scenes = new string[] { };
			return playerOptions;
		}
	}
}