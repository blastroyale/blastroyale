using System.Threading.Tasks;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class PlayFabIntegrationTests : IntegrationTestFixture
	{
		private Statechart.Statechart _chart;
		private AuthenticationState _state;
		
		private void SetupChart()
		{
			FeatureFlags.EMAIL_AUTH = false; // make things simpler :D 
			_state = new AuthenticationState(TestLogic, TestServices, TestUI, TestData, TestNetwork, e => _chart.Trigger(e), TestConfigs);
			_chart = new Statechart.Statechart(_state.Setup);
			_chart.Run();
		}

	
		[Test]
		public void TestInitialState()
		{
			SetupChart();
			Assert.AreEqual("Login Device Authentication", _chart.CurrentState);
		}
		
		[Test]
		public async Task TestRegistering()
		{
			SetupChart();
			_state.LoginWithDevice();
			
			await WaitFor(() => _chart.CurrentState == "Login");
			
			Assert.AreEqual("Login", _chart.CurrentState); // TODO: Fix me i only pass every few tries
		}
	}
}