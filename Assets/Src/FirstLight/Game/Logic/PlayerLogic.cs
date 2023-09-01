using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's fundamental data
	/// </summary>
	public interface IPlayerDataProvider
	{
		/// <summary>
		/// Request the player's current trophy count.
		/// </summary>
		IObservableFieldReader<uint> Trophies { get; }

		/// <summary>
		/// Request the player's current fame level
		/// </summary>
		IObservableFieldReader<uint> Level { get; }

		/// <summary>
		/// Request the player's current XP (level up consumes XP).
		/// </summary>
		IObservableFieldReader<uint> XP { get; }

		/// <summary>
		/// Requests the unlock level of the given <paramref name="unlockSystem"/>
		/// </summary>
		uint GetUnlockSystemLevel(UnlockSystem unlockSystem);

		/// <summary>
		/// Returns a list of rewards you get for reaching a specific level.
		/// </summary>
		List<IReward> GetRewardsForLevel(uint level);

		/// <summary>
		/// Checks if a given player has completed a given tutorial step
		/// </summary>
		bool HasTutorialSection(TutorialSection section);

		/// <summary>
		/// Checks if this account has completed guest data migration
		/// </summary>
		bool MigratedGuestAccount { get; }
	}

	/// <inheritdoc />
	public interface IPlayerLogic : IPlayerDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="amount"/> of XP to the Player.
		/// </summary>
		void AddXP(uint amount);

		/// <summary>
		/// Updates player's trophies (Elo) based on their ranking in the match, and returns the amount of trophies
		/// added/removed
		/// </summary>
		void UpdateTrophies(int change);

		/// <summary>
		/// Flags that the given tutorial step is completed
		/// </summary>
		void MarkTutorialSectionCompleted(TutorialSection section);

		/// <summary>
		/// Marks the guest migration status, meaning the player will never be able to migrate guest data into the
		/// account upon logging in again
		/// </summary>
		void MarkGuestAccountMigrated();

		void ResetLevelAndXP();
	}

	// TODO: Remove all player skin stuff related and move to CollectionLogic
	/// <inheritdoc cref="IPlayerLogic"/>
	public class PlayerLogic : AbstractBaseLogic<PlayerData>, IPlayerLogic, IGameLogicInitializer
	{
		private IObservableField<uint> _trophies;
		private IObservableField<uint> _level;
		private IObservableField<uint> _xp;

		private IObservableField<TutorialSection> _tutorialSections;

		public IObservableFieldReader<TutorialSection> TutorialSections => _tutorialSections;

		/// <inheritdoc />
		public IObservableFieldReader<uint> Trophies => _trophies;

		public IObservableFieldReader<uint> Level => _level;
		public IObservableFieldReader<uint> XP => _xp;

		public bool MigratedGuestAccount
		{
			get
			{
				var data = DataProvider.GetData<PlayerData>();
				return data.MigratedGuestData;
			}
		}

		// TODO - Remove appdata/any local data call from game logic so it doesn't have to be copied onto backend code
		private AppData AppData => DataProvider.GetData<AppData>();

		public PlayerLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_trophies = new ObservableResolverField<uint>(() => Data.Trophies, val => Data.Trophies = val);
			_level = new ObservableResolverField<uint>(() => Data.Level, val => Data.Level = val);
			_xp = new ObservableResolverField<uint>(() => Data.Xp, val => Data.Xp = val);
			_tutorialSections = new ObservableField<TutorialSection>(DataProvider.GetData<TutorialData>().TutorialSections);
		}

		public void ReInit()
		{
			{
				var listeners = _trophies.GetObservers();
				_trophies = new ObservableResolverField<uint>(() => Data.Trophies, val => Data.Trophies = val);
				_trophies.AddObservers(listeners);
			}

			{
				var listeners = _level.GetObservers();
				_level = new ObservableResolverField<uint>(() => Data.Level, val => Data.Level = val);
				_level.AddObservers(listeners);
			}

			{
				var listeners = _xp.GetObservers();
				_xp = new ObservableResolverField<uint>(() => Data.Xp, val => Data.Xp = val);
				_xp.AddObservers(listeners);
			}

			{
				var listeners = _tutorialSections.GetObservers();
				_tutorialSections = new ObservableField<TutorialSection>(DataProvider.GetData<TutorialData>().TutorialSections);
				_tutorialSections.AddObservers(listeners);
			}

			_trophies.InvokeUpdate();
			_level.InvokeUpdate();
			_xp.InvokeUpdate();
			_tutorialSections.InvokeUpdate();
		}

		/// <inheritdoc />
		public uint GetUnlockSystemLevel(UnlockSystem unlockSystem)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsList<PlayerLevelConfig>();

			for (var i = 0; i < configs.Count; i++)
			{
				if (configs[i].Systems.Contains(unlockSystem))
				{
					return configs[i].Level;
				}
			}

			throw new LogicException($"The system {unlockSystem} is not defined in the {nameof(PlayerLevelConfig)}");
		}

		public List<IReward> GetRewardsForLevel(uint level)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<PlayerLevelConfig>((int) level);
			var rewards = new List<IReward>();

			foreach (var (id, amount) in config.Rewards)
			{
				rewards.Add(new CurrencyReward(id, (uint) amount));
			}

			foreach (var unlockSystem in config.Systems)
			{
				rewards.Add(new UnlockReward(unlockSystem));
			}

			return rewards;
		}

		/// <inheritdoc />
		public void AddXP(uint amount)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();
			var xp = _xp.Value + amount;
			var level = _level.Value;

			if (level == configs.Count + 1)
			{
				return;
			}

			while (configs.TryGetValue((int) level, out var config) && xp >= config.LevelUpXP)
			{
				xp -= config.LevelUpXP;
				level++;
			}

			_level.Value = level;
			_xp.Value = level >= configs.Count ? 0 : xp;
		}

		/// <inheritdoc />
		public void UpdateTrophies(int change)
		{
			_trophies.Value = (uint) Math.Max(0, _trophies.Value + change);
		}

		/// <inheritdoc />
		public bool HasTutorialSection(TutorialSection section)
		{
			return DataProvider.GetData<TutorialData>().TutorialSections.HasFlag(section);
		}

		/// <inheritdoc />
		public void MarkTutorialSectionCompleted(TutorialSection section)
		{
			var data = DataProvider.GetData<TutorialData>();
			data.TutorialSections |= section;
			_tutorialSections.Value = data.TutorialSections; // trigger observables after bitshift
		}

		/// <inheritdoc />
		public void MarkGuestAccountMigrated()
		{
			var data = DataProvider.GetData<PlayerData>();
			data.MigratedGuestData = true;
		}

		public void ResetLevelAndXP()
		{
			_level.Value = 1;
			_xp.Value = 0;
		}
	}
}