using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;


namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Screen, where players can equip items and upgrade loot.
	/// </summary>
	public class LeaderboardScreenPresenter : AnimatedUiPresenterData<LeaderboardScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action BackClicked;
		}

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void OnSetData()
		{
			base.OnSetData();
		}

		protected override void OnOpened()
		{
			base.OnOpened();
		}
	}
}