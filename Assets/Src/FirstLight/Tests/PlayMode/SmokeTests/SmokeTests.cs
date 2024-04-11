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
		//[TestCase(typeof(TutorialTestCase), ExpectedResult = null)]
		//[TestCase(typeof(TenMatchesInARow), ExpectedResult = null)]
		[TestCase(typeof(TwoMatchesInARow), ExpectedResult = null)]
		[Timeout(1000 * 60 * 10)]
		public IEnumerator RunCase(Type testCase)
		{
			MainInstaller.Clean();
			var runner = FLGTestRunner.Instance;
			runner.FailInstruction = Fail;
			var instance = Activator.CreateInstance(testCase);
			yield return runner.RunInUnitTest((PlayTestCase)instance);
			yield return new ExitPlayMode();
		}
		

		

		public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
		{
			playerOptions.scenes = new string[] { };
			return playerOptions;
		}
	}
}