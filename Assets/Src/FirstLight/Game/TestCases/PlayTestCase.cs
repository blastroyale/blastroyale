using System.Collections;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.TestCases.Helpers;
using FirstLight.Game.Utils;


namespace FirstLight.Game.TestCases
{
	public abstract class PlayTestCase
	{
		protected TestInstaller _installer;
		protected FLGTestRunner Runner { get; set; }

		// If true doesn't quit game after finished
		public virtual bool IsAutomation => false;

		public void SetInstaller(TestInstaller installer)
		{
			_installer = installer;
		}


		protected QuantumHelper Quantum => _installer.Resolve<QuantumHelper>();
		protected AccountHelper Account => _installer.Resolve<AccountHelper>();
		protected FeatureFlagsHelper FeatureFlags => _installer.Resolve<FeatureFlagsHelper>();
		protected UIHelper UIGeneric => _installer.Resolve<UIHelper>();
		protected BattlePassUIHelper UIBattlepass => _installer.Resolve<BattlePassUIHelper>();
		protected GameUIHelper UIGame => _installer.Resolve<GameUIHelper>();
		protected HomeUIHelper UIHome => _installer.Resolve<HomeUIHelper>();
		protected GamemodeUIHelper UIGamemode => _installer.Resolve<GamemodeUIHelper>();
		protected PlayerConfigsHelper PlayerConfigs => _installer.Resolve<PlayerConfigsHelper>();


		// Game stuff
		protected MessageBrokerHelper MessageBroker => _installer.Resolve<MessageBrokerHelper>();
		protected IGameServices Services => MainInstaller.ResolveServices();
		protected IGameDataProvider DataProvider => MainInstaller.Resolve<IGameDataProvider>();


		public abstract IEnumerator Run();

		public virtual void OnGameAwaken()
		{
		}
	}
}