using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
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
		/// Request the player's current trophy count.
		/// </summary>
		IObservableFieldReader<uint> Trophies { get; }
		
		/// <summary>
		/// Requests a list of systems already seen by the player.
		/// </summary>
		IObservableList<UnlockSystem> SystemsTagged { get; }

		/// <summary>
		/// Requests the current <see cref="Infos.PlayerInfo"/>
		/// </summary>
		PlayerInfo PlayerInfo { get; }

		/// <summary>
		/// Requests the unlock level of the given <paramref name="unlockSystem"/>
		/// </summary>
		uint GetUnlockSystemLevel(UnlockSystem unlockSystem);

		/// <summary>
		/// Requests the list of unlocked systems until the given <paramref name="level"/> from the given <paramref name="startLevel"/>
		/// </summary>
		List<UnlockSystem> GetUnlockSystems(uint level, uint startLevel = 1);
	}

	/// <inheritdoc />
	public interface IPlayerLogic : IPlayerDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="amount"/> of XP to the Player.
		/// </summary>
		void AddXp(uint amount);
		
		/// <summary>
		/// Updates player's trophies (Elo) based on their ranking in the match, and returns the amount of trophies
		/// added/removed
		/// </summary>
		void UpdateTrophies(int change);
		
		/// <summary>
		/// Changes the player skin to <paramref name="skin"/>
		/// </summary>
		void ChangePlayerSkin(GameId skin);
	}
	
	/// <inheritdoc cref="IPlayerLogic"/>
	public class PlayerLogic : AbstractBaseLogic<PlayerData>, IPlayerLogic, IGameLogicInitializer
	{
		private IObservableField<uint> _trophies;

		/// <inheritdoc />
		public IObservableFieldReader<uint> Trophies => _trophies;
		
		/// <inheritdoc />
		public IObservableList<UnlockSystem> SystemsTagged { get; private set; }

		/// <inheritdoc />
		public PlayerInfo PlayerInfo
		{
			get
			{
				var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();
				var config = configs[(int) Math.Min(Data.Level, configs.Count)];
				var maxLevel = configs[configs.Count].Level + 1;
				var totalXp = Data.Level;

				for (var i = 1; i < Data.Level; i++)
				{
					totalXp += configs[i].LevelUpXP;
				}
				
				return new PlayerInfo
				{
					Level = Data.Level,
					Xp = Data.Xp,
					TotalCollectedXp = totalXp,
					MaxLevel = maxLevel,
					Config = config,
					Skin = Data.PlayerSkinId,
					DeathMarker = Data.DeathMarker,
					TotalTrophies = _trophies.Value,
					CurrentUnlockedSystems = GetUnlockSystems(Data.Level)
				};
			}
		}

		private AppData AppData => DataProvider.GetData<AppData>();

		public PlayerLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_trophies = new ObservableResolverField<uint>(() => Data.Trophies, val => Data.Trophies = val);
			SystemsTagged = new ObservableList<UnlockSystem>(AppData.SystemsTagged);
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
			var xp = Data.Xp + amount;
			var level = Data.Level;
			
			if (level == configs.Count + 1)
			{
				return;
			}

			while (configs.TryGetValue((int) level, out var config) && xp >= config.LevelUpXP)
			{
				xp -= config.LevelUpXP;
				level++;
			}

			Data.Level = level;
			Data.Xp = level >= configs.Count ? 0 : xp;
		}

		/// <inheritdoc />
		public void UpdateTrophies(int change)
		{
			_trophies.Value = (uint) Math.Max(0, _trophies.Value + change);
		}

		/// <inheritdoc />
		public void ChangePlayerSkin(GameId skin)
		{
			if (!skin.IsInGroup(GameIdGroup.PlayerSkin))
			{
				throw new LogicException($"Skin Id '{skin.ToString()}' is not part of the Game Id Group PlayerSkin.");
			}

			Data.PlayerSkinId = skin;
		}
	}
}