using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
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
		List<ItemData> GetRewardsForFameLevel(uint level);

		/// <summary>
		/// Checks if a given player has completed a given tutorial step
		/// </summary>
		bool HasTutorialSection(TutorialSection section);

		/// <summary>
		/// Checks if this account has completed guest data migration
		/// </summary>
		bool MigratedGuestAccount { get; }

		/// <summary>
		/// Gets the amount of XP needed to level up
		/// </summary>
		uint GetXpNeededForLevel(uint level);
		
		/// <summary>
		/// Returns the flags of the player.
		/// </summary>
		PlayerFlags Flags { get; }
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
	
	/// <inheritdoc cref="IPlayerLogic"/>
	public class PlayerLogic : AbstractBaseLogic<PlayerData>, IPlayerLogic, IGameLogicInitializer
	{
		private IObservableField<uint> _trophies;
		private IObservableField<uint> _level;
		private IObservableField<uint> _xp;

		private IObservableField<TutorialSection> _tutorialSections;

		/// <inheritdoc />
		public IObservableFieldReader<uint> Trophies => _trophies;

		public IObservableFieldReader<uint> Level => _level;
		public IObservableFieldReader<uint> XP => _xp;
		public PlayerFlags Flags => Data.Flags;

		public bool MigratedGuestAccount
		{
			get
			{
				var data = DataProvider.GetData<PlayerData>();
				return data.MigratedGuestData;
			}
		}

		public PlayerLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}
		
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
		
		public uint GetUnlockSystemLevel(UnlockSystem unlockSystem)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();

			foreach (var config in configs)
			{
				for (var i = config.Value.LevelStart; i <= config.Value.LevelEnd; i++)
				{
					if (config.Value.Systems.Contains(unlockSystem))
					{
						// In the config we specify fame points required and rewards in the current level
						// Example: in config in level 1 we set reward Shop and requirement 100 XP
						// It means that to get from level 1 to level 2 a player needs to get 100 XP and will be rewarded with Shop
						return config.Value.LevelStart + 1;
					}
				}
			}
			
			FLog.Info($"The system {unlockSystem} is not defined in the {nameof(PlayerLevelConfig)}");
			
			return 0;
		}
		
		public List<ItemData> GetRewardsForFameLevel(uint level)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();
			var rewards = new List<ItemData>();

			foreach (var config in configs)
			{
				if (level >= config.Value.LevelStart && level <= config.Value.LevelEnd)
				{
					foreach (var (id, amount) in config.Value.Rewards)
					{
						if(amount > 1)
							rewards.Add(ItemFactory.Currency(id, amount));
						else
							rewards.Add(ItemFactory.Simple(id));
					}

					foreach (var unlockSystem in config.Value.Systems)
					{
						rewards.Add(ItemFactory.Unlock(unlockSystem));
					}
					
					break;
				}
			}

			return rewards;
		}

		public uint GetXpNeededForLevel(uint level)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();

			foreach (var config in configs)
			{
				if (level >= config.Value.LevelStart && level <= config.Value.LevelEnd)
				{
					return config.Value.LevelUpXP;
				}
			}
			
			throw new LogicException($"Could not find level config for level {level}");
		}

		public void AddXP(uint amount)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();
			var xp = _xp.Value + amount;
			var level = _level.Value;
			
			if (level == GameConstants.Data.PLAYER_FAME_MAX_LEVEL)
			{
				return;
			}

			foreach (var config in configs)
			{
				if (level < config.Value.LevelStart || level > config.Value.LevelEnd)
				{
					continue;
				}
				
				for (var i = config.Value.LevelStart; i <= config.Value.LevelEnd; i++)
				{
					if (xp < config.Value.LevelUpXP)
					{
						break;
					}
					
					xp -= config.Value.LevelUpXP;
					level++;
				}
				
				if (xp < config.Value.LevelUpXP)
				{
					break;
				}
			}
			for (var l = _level.Value; l < level; l++)
			{
				GameLogic.RewardLogic.Reward(GetRewardsForFameLevel(l));
			}

			_level.Value = level;
			_xp.Value = level >= GameConstants.Data.PLAYER_FAME_MAX_LEVEL ? 0 : xp;
		}

		public void UpdateTrophies(int change)
		{
			_trophies.Value = (uint) Math.Max(0, _trophies.Value + change);
		}
		
		public bool HasTutorialSection(TutorialSection section)
		{
			return DataProvider.GetData<TutorialData>().TutorialSections.HasFlag(section);
		}

		public void MarkTutorialSectionCompleted(TutorialSection section)
		{
			var data = DataProvider.GetData<TutorialData>();
			data.TutorialSections |= section;
			_tutorialSections.Value = data.TutorialSections; // trigger observables after bitshift
		}
		
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