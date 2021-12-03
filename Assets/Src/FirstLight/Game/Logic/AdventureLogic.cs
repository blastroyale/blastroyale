using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using FirstLight.Services;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the game's app
	/// </summary>
	public interface IAdventureDataProvider
	{
		/// <summary>
		/// Requests the player's current selected Id
		/// </summary>
		IObservableField<int> SelectedMapId { get; }
		/// <summary>
		/// Requests the player's current selected slot <see cref="MapConfig"/>.
		/// </summary>
		MapConfig SelectedMapConfig { get; }
		
		/// <summary>
		/// Increments the current's selected map level
		/// </summary>
		void IncrementMapLevel();
	}
	
	/// <inheritdoc />
	public interface IAdventureLogic : IAdventureDataProvider
	{
	}
	
	/// <inheritdoc cref="IAppLogic"/>
	public class AdventureLogic : AbstractBaseLogic<PlayerData>, IAdventureLogic, IGameLogicInitializer
	{
		/// <inheritdoc />
		public IObservableField<int> SelectedMapId { get; private set; }
		/// <inheritdoc />
		public MapConfig SelectedMapConfig => GameLogic.ConfigsProvider.GetConfig<MapConfig>(SelectedMapId.Value);
		
		private AppData AppData => DataProvider.GetData<AppData>();

		public AdventureLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>();
			
			SelectedMapId = new ObservableField<int>(configs[configs.Count - 1].Id);
		}
		
		/// <inheritdoc />
		public void IncrementMapLevel()
		{
			if (SelectedMapId.Value + 1 ==
			    GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>().Count)
			{
				SelectedMapId.Value = GameConstants.OnboardingAdventuresCount + GameConstants.FtueAdventuresCount;
			}
			else
			{
				SelectedMapId.Value++;
			}
		}

	}
}