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
		IObservableField<int> AdventureSelectedId { get; }
		/// <summary>
		/// Requests the player's current selected slot <see cref="AdventureInfo"/>.
		/// </summary>
		AdventureInfo AdventureSelectedInfo { get; }
		/// <summary>
		/// Requests the completed adventure Ids that the player has already seen
		/// </summary>
		IObservableList<int> AdventuresCompletedTagged { get; }

		/// <summary>
		/// Requests the <see cref="AllAdventuresInfo"/> of all adventure slots available to the player
		/// </summary>
		/// <returns></returns>
		AllAdventuresInfo GetAllAdventuresInfo();

		/// <summary>
		/// Get's the <see cref="AdventureInfo"/> for the given <paramref name="adventureId"/>
		/// </summary>
		AdventureInfo GetInfo(int adventureId);

		/// <summary>
		/// Increments the current player level
		/// </summary>
		void IncrementLevel();
	}
	
	/// <inheritdoc />
	public interface IAdventureLogic : IAdventureDataProvider
	{
		/// <summary>
		/// Marks the given <see cref="adventureId"/> as completed with the given <paramref name="success"/> state
		/// </summary>
		void MarkAdventureCompleted(int adventureId, bool success);
		
		/// <summary>
		/// Marks the given <paramref name="adventureId"/> with it's first time rewards collected
		/// </summary>
		void MarkFirstTimeRewardsCollected(int adventureId);
	}
	
	/// <inheritdoc cref="IAppLogic"/>
	public class AdventureLogic : AbstractBaseLogic<PlayerData>, IAdventureLogic, IGameLogicInitializer
	{
		/// <inheritdoc />
		public IObservableField<int> AdventureSelectedId { get; private set; }
		/// <inheritdoc />
		public IObservableList<int> AdventuresCompletedTagged { get; private set; }
		/// <inheritdoc />
		public AdventureInfo AdventureSelectedInfo => GetInfo(AdventureSelectedId.Value);
		
		private AppData AppData => DataProvider.GetData<AppData>();

		public AdventureLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<AdventureConfig>();

			AdventuresCompletedTagged = new ObservableList<int>(AppData.AdventuresCompletedTagged);
			
			foreach (var pair in configs)
			{
				if (!TryGetAdventureData(pair.Key, out _, out _))
				{
					AdventureSelectedId = new ObservableField<int>(pair.Key);

					return;
				}
			}
			
			AdventureSelectedId = new ObservableField<int>(configs[configs.Count - 1].Id);
		}
		
		/// <inheritdoc />
		public AllAdventuresInfo GetAllAdventuresInfo()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<AdventureConfig>();
			var allInfo = new AllAdventuresInfo
			{
				Adventures = new List<AdventureInfo>(),
				DifficultiesStartId = new int[(int) AdventureDifficultyLevel.TOTAL]
			};

			allInfo.DifficultiesStartId[0] = 0;

			foreach (var pair in configs)
			{
				var info = GetInfo(pair.Key);
				
				allInfo.Adventures.Add(info);

				if (!allInfo.NextAdventure.IsUnlocked && info.IsUnlocked && !info.IsCompleted)
				{
					allInfo.NextAdventure = info;
				}

				if (AdventureSelectedId.Value == info.AdventureData.Id)
				{
					allInfo.SelectedAdventure = info;
				}

				if (info.Config.Difficulty != AdventureDifficultyLevel.Normal && allInfo.DifficultiesStartId[(int) info.Config.Difficulty] == 0)
				{
					allInfo.DifficultiesStartId[(int) info.Config.Difficulty] = info.AdventureData.Id;
				}
			}

			return allInfo;
		}

		/// <inheritdoc />
		public AdventureInfo GetInfo(int adventureId)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<AdventureConfig>(adventureId);
			var requirementId = config.UnlockedAdventureRequirement;

			TryGetAdventureData(adventureId, out _, out var adventureData);

			return new AdventureInfo
			{
				AdventureData = adventureData,
				Config = config,
				IsUnlocked = requirementId < 0 || 
				             TryGetAdventureData(requirementId, out _, out var requiredData) && 
				             requiredData.KillCount > 0
			};
		}

		/// <inheritdoc />
		public void IncrementLevel()
		{
			if (AdventureSelectedId.Value + 1 ==
			    GameLogic.ConfigsProvider.GetConfigsDictionary<AdventureConfig>().Count)
			{
				AdventureSelectedId.Value = GameConstants.OnboardingAdventuresCount + GameConstants.FtueAdventuresCount;
			}
			else
			{
				AdventureSelectedId.Value++;
			}
		}

		/// <inheritdoc />
		public void MarkAdventureCompleted(int adventureId, bool success)
		{
			if (!TryGetAdventureData(adventureId, out var index, out var adventureData))
			{
				index = Data.AdventureProgress.Count;
				
				Data.AdventureProgress.Add(adventureData);
			}

			if (success)
			{
				adventureData.KillCount++;

				Data.AdventureProgress[index] = adventureData;
			}
		}

		/// <inheritdoc />
		public void MarkFirstTimeRewardsCollected(int adventureId)
		{
			if (!TryGetAdventureData(adventureId, out var index, out var data) || data.KillCount == 0 || data.RewardCollected)
			{
				throw new LogicException($"The adventure {adventureId} is not ready to collect it's first time rewards. " +
				                         $"Completion: {data.KillCount > 0}, Reward state: {data.RewardCollected}");
			}

			data.RewardCollected = true;

			Data.AdventureProgress[index] = data;
		}

		private bool TryGetAdventureData(int adventureId, out int index, out AdventureData data)
		{
			var progress = Data.AdventureProgress;
			
			index = progress.FindIndex(adventure => adventure.Id == adventureId);
			data = index >= 0 ? progress[index] : new AdventureData { Id = adventureId, KillCount = 0 };

			return index >= 0;
		}
	}
}