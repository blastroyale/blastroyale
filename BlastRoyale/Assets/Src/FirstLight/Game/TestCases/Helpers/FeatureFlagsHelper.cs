using System;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class FeatureFlagsHelper : TestHelper
	{
		private Action OverwriteFeatureFlags;

		public FeatureFlagsHelper(FLGTestRunner testRunner) : base(testRunner)
		{
			RunWhenGameAwake(Subscribe);
		}

		public void SetTutorial(bool tutorial)
		{
			OverwriteFeatureFlags += () => { FeatureFlags.TUTORIAL = tutorial; };
		}
		

		public void Subscribe()
		{
			Services.MessageBrokerService.Subscribe<FeatureFlagsReceived>(changed => { OverwriteFeatureFlags?.Invoke(); });
		}

		public void FreshGameInstallation()
		{
			PlayerPrefs.DeleteAll();
		}
	}
}