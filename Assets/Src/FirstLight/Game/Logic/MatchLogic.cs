using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the game's app
	/// </summary>
	public interface IMatchDataProvider
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
		/// Request the player's current trophy count.
		/// </summary>
		IObservableFieldReader<uint> Trophies { get; }
	}

	/// <inheritdoc />
	public interface IMatchLogic : IMatchDataProvider
	{
		void UpdateTrophies(QuantumPlayerMatchData[] players, QuantumPlayerMatchData localPlayer);
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class MatchLogic : AbstractBaseLogic<PlayerData>, IMatchLogic, IGameLogicInitializer
	{
		/// <inheritdoc />
		public IObservableField<int> SelectedMapId { get; private set; }

		/// <inheritdoc />
		public MapConfig SelectedMapConfig => GameLogic.ConfigsProvider.GetConfig<MapConfig>(SelectedMapId.Value);

		public IObservableFieldReader<uint> Trophies { get; private set; }

		private AppData AppData => DataProvider.GetData<AppData>();

		public MatchLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>();

			SelectedMapId = new ObservableField<int>(configs[configs.Count - 1].Id);

			// Trophies = new ObservableField<uint>(Data.Trophies);
			Trophies = new ObservableField<uint>(666);
		}

		public void UpdateTrophies(QuantumPlayerMatchData[] players, QuantumPlayerMatchData localPlayer)
		{
			// TODO: Implement trophy calculations
			Data.Trophies += 10 * localPlayer.PlayerRank;
		}
	}
}