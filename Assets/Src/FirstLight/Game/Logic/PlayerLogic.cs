using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's fundamental data
	/// </summary>
	public interface IPlayerDataProvider
	{

		/// <summary>
		/// Requests the player's level
		/// </summary>
		IObservableFieldReader<uint> Level { get; }
		/// <summary>
		/// Requests the player's XP
		/// </summary>
		IObservableFieldReader<uint> Xp { get; }
		/// <summary>
		/// Requests the player's current selected skin
		/// </summary>
		IObservableFieldReader<GameId> CurrentSkin { get; }
		/// <summary>
		/// Requests a list of Emoji owned by the player.
		/// </summary>
		IObservableListReader<GameId> Emoji { get; }
		/// <summary>
		/// Requests the <see cref="PlayerInfo"/> representing the current player
		/// </summary>
		PlayerInfo CurrentLevelInfo { get; }
		/// <summary>
		/// Requests the current list of unlocked systems
		/// </summary>
		List<UnlockSystem> CurrentUnlockedSystems { get; }
		/// <summary>
		/// Requests a list of systems already seen by the player.
		/// </summary>
		IObservableList<UnlockSystem> SystemsTagged { get; }

		/// <summary>
		/// Requests the <see cref="PlayerInfo"/> for the given player <paramref name="level"/>
		/// </summary>
		public PlayerInfo GetInfo(uint level);

		/// <summary>
		/// Requests the unlock level of the given <paramref name="unlockSystem"/>
		/// </summary>
		public uint GetUnlockSystemLevel(UnlockSystem unlockSystem);

		/// <summary>
		/// Requests the list of unlocked systems until the given <paramref name="level"/> from the given <paramref name="startLevel"/>
		/// </summary>
		public List<UnlockSystem> GetUnlockSystems(uint level, uint startLevel = 1);
	}

	/// <inheritdoc />
	public interface IPlayerLogic : IPlayerDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="amount"/> of XP to the Player.
		/// </summary>
		void AddXp(uint amount);
		/// <summary>
		/// 
		/// Changes the player skin to <paramref name="skin"/>
		/// </summary>
		void ChangePlayerSkin(GameId skin);
	}
	
	/// <inheritdoc cref="IPlayerLogic"/>
	public class PlayerLogic : AbstractBaseLogic<PlayerData>, IPlayerLogic, IGameLogicInitializer
	{
		public static string DefaultPlayerName = "Player Name";
		
		private IObservableField<uint> _level;
		private IObservableField<uint> _xp;
		private IObservableList<GameId> _emoji;
		private IObservableField<GameId> _currentSkin;
		
		/// <inheritdoc />
		public IObservableFieldReader<uint> Level => _level;
		/// <inheritdoc />
		public IObservableFieldReader<uint> Xp => _xp;
		/// <inheritdoc />
		public IObservableListReader<GameId> Emoji => _emoji;
		/// <inheritdoc />
		public IObservableFieldReader<GameId> CurrentSkin => _currentSkin;
		/// <inheritdoc />
		public PlayerInfo CurrentLevelInfo => GetInfo(_level.Value);
		/// <inheritdoc />
		public List<UnlockSystem> CurrentUnlockedSystems => GetUnlockSystems(_level.Value);
		/// <inheritdoc />
		public IObservableList<UnlockSystem> SystemsTagged { get; private set; }

		private AppData AppData => DataProvider.GetData<AppData>();

		public PlayerLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_level = new ObservableResolverField<uint>(() => Data.Level, level => Data.Level = level);
			_xp = new ObservableResolverField<uint>(() => Data.Xp, xp => Data.Xp = xp);
			_emoji = new ObservableList<GameId>(Data.Emoji);
			_currentSkin = new ObservableResolverField<GameId>(() => Data.PlayerSkinId,skin => Data.PlayerSkinId =skin);
			SystemsTagged = new ObservableList<UnlockSystem>(AppData.SystemsTagged);
		}

		/// <inheritdoc />
		public PlayerInfo GetInfo(uint level)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();
			var config = configs[(int) Math.Min(level, configs.Count)];
			var maxLevel = configs[configs.Count].Level + 1;
			var totalXp = _xp.Value;

			for (var i = 1; i < _level.Value; i++)
			{
				totalXp += configs[i].LevelUpXP;
			}
				
			return new PlayerInfo
			{
				Level = level,
				Xp = _xp.Value,
				TotalCollectedXp = totalXp,
				MaxLevel = maxLevel,
				Config = config,
				Skin = _currentSkin.Value
			};
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

		/// <inheritdoc />
		public List<UnlockSystem> GetUnlockSystems(uint level, uint startLevel = 1)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsList<PlayerLevelConfig>();
			var ret = new List<UnlockSystem>();

			for (var i = 0; i < configs.Count; i++)
			{
				if (configs[i].Level < startLevel)
				{
					continue;
				}
				
				if (configs[i].Level > level)
				{
					break;
				}
				
				ret.AddRange(configs[i].Systems);
			}

			return ret;
		}

		/// <inheritdoc />
		public void AddXp(uint amount)
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

			if (_level.Value != level)
			{
				_level.Value = level;
			}

			_xp.Value = level >= configs.Count ? 0 : xp;
		}

		/// <inheritdoc />
		public void ChangePlayerSkin(GameId skin)
		{
			if (!skin.IsInGroup(GameIdGroup.PlayerSkin))
			{
				throw new LogicException($"Skin Id '{skin}' is not part of the Game Id Group PlayerSkin.");
			}

			_currentSkin.Value = skin;
		}
	}
}