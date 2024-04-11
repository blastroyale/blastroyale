using System.Collections;
using System.Linq;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.TestCases.Helpers;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace FirstLight.Game.TestCases

{
	public class JoinTestRoom : PlayTestCase
	{
		public override bool IsAutomation => true;

		public override void AfterGameAwaken()
		{
			FeatureFlags.SetTutorial(false);
		}

		public override IEnumerator Run()
		{
			yield return UIHome.WaitHomePresenter(60, 0);
			yield return UIHome.ClickGameMode();
			yield return UIGamemode.ClickCustomRoom();
			yield return UIGamemode.SelectGamemodeWithName("Testing");
			yield return UIGamemode.ClickCreate();
		}
	}
}