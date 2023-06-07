using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum.Systems.Bots
{
	/// <summary>
	/// Handles bot creation, difficulty settings and spawn location
	/// </summary>
	public unsafe class BotSetup
	{
		private class BotSetupContext
		{
			public int TeamSize;
			public List<EntityComponentPointerPair<PlayerSpawner>> PlayerSpawners;
			public List<int> BotNamesIndices;
			public IList<GameId> DeathMakers;
			public IList<GameId> Gliders;
			public GameId[] SkinOptions;
			public List<GameId> WeaponsPool;
			public List<QuantumBotConfig> BotConfigs;
			public uint AverageTrophies;

			public EntityPrototype PlayerPrototype;
			public NavMeshAgentConfig NavMeshAgentConfig;
			public uint TotalTeamsInGameMode;
			public Dictionary<int, List<EntityRef>> PlayersByTeam;
		}


		internal void InitializeBots(Frame f, uint baseTrophiesAmount)
		{
			if (!f.Context.GameModeConfig.AllowBots || f.ComponentCount<BotCharacter>() > 0)
			{
				return;
			}

			var playerLimit = f.PlayerCount;
			var botIds = new List<PlayerRef>();

			var maxPlayers = Math.Min(f.Context.MapConfig.MaxPlayers, f.Context.GameModeConfig.MaxPlayers);
			if (playerLimit == 1 && maxPlayers > 1) // offline game with bots
			{
				playerLimit = (int)maxPlayers;
			}

			for (var i = 0; i < playerLimit; i++)
			{
				if (i >= f.PlayerCount || (f.GetPlayerInputFlags(i) & DeterministicInputFlags.PlayerNotPresent) ==
					DeterministicInputFlags.PlayerNotPresent)
				{
					botIds.Add(i);
				}
			}

			if (botIds.Count != playerLimit)
			{
				AddBots(f, botIds, baseTrophiesAmount);
			}
		}

		private BotSetupContext GetBotContext(Frame f, uint baseTrophies)
		{
			var botItems = GameIdGroup.BotItem.GetIds();
			var ctx = new BotSetupContext()
			{
				TeamSize = f.Context.GameModeConfig.Teams ? (int)f.Context.GameModeConfig.MaxPlayersInTeam : 1,
				PlayerSpawners = GetFreeSpawnPoints(f),
				BotNamesIndices = Enumerable.Range(1, f.GameConfig.BotsNameCount).ToList(),
				DeathMakers = GameIdGroup.DeathMarker.GetIds(),
				Gliders = GameIdGroup.Glider.GetIds(),
				SkinOptions = GameIdGroup.PlayerSkin.GetIds().Where(item => botItems.Contains(item)).ToArray(),
				WeaponsPool = GameIdGroup.Weapon.GetIds().Where(id => id != GameId.Hammer).ToList(),
				BotConfigs = GetBotConfigsList(f, baseTrophies),
				AverageTrophies = baseTrophies,
				PlayerPrototype = f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id),
				NavMeshAgentConfig = f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id),
				PlayersByTeam = GetPlayersByTeam(f),
				TotalTeamsInGameMode = f.Context.GameModeConfig.MaxPlayers / (f.Context.GameModeConfig.Teams ? f.Context.GameModeConfig.MaxPlayersInTeam : 1)
			};
			AddBotTeams(ctx);
			return ctx;
		}

		private void AddBots(Frame f, List<PlayerRef> playerRefs, uint baseTrophies)

		{
			var ctx = GetBotContext(f, baseTrophies);
			if (ctx.BotConfigs.Count == 0)
			{
				throw new Exception("Bot configs not found for this game!");
			}

			var forcedBotTypes = new List<BotBehaviourType>();
			foreach (var playerSpawner in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (playerSpawner.Component->SpawnerType == SpawnerType.BotOfType)
				{
					forcedBotTypes.Add(playerSpawner.Component->BehaviourType);
				}
			}

			foreach (var playerRef in playerRefs)
			{
				var botConfig = f.RNG->RandomElement(ctx.BotConfigs);

				// If there are spawns for specific types of bots, we use those
				var forcedTypeConfigIndex = -1;
				while (forcedBotTypes.Count > 0 && forcedTypeConfigIndex == -1)
				{
					forcedTypeConfigIndex = ctx.BotConfigs.FindIndex(config => config.BehaviourType == forcedBotTypes[0]);
					forcedBotTypes.RemoveAt(0);
				}

				if (forcedTypeConfigIndex > -1)
				{
					botConfig = ctx.BotConfigs[forcedTypeConfigIndex];
				}


				AddBot(f, ctx, playerRef, botConfig);
			}
		}

		private void AddBot(Frame f, BotSetupContext ctx, PlayerRef id, QuantumBotConfig config)
		{
			var teamId = GetBotTeamId(f, id, ctx.PlayersByTeam);
			var rngSpawnIndex = GetSpawnPointIndexByTypeOfBot(f, ctx.PlayerSpawners, config.BehaviourType);
			var spawnerTransform = f.Get<Transform3D>(ctx.PlayerSpawners[rngSpawnIndex].Entity);
			var botEntity = f.Create(ctx.PlayerPrototype);
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
			var navMeshAgent = new NavMeshSteeringAgent();
			var pathfinder = NavMeshPathfinder.Create(f, botEntity, ctx.NavMeshAgentConfig);
			var listNamesIndex = f.RNG->RandomElement(ctx.BotNamesIndices);

			if (!ctx.PlayersByTeam.TryGetValue(teamId, out var entities))
			{
				entities = new List<EntityRef>();
				ctx.PlayersByTeam[teamId] = entities;
			}


			entities.Add(botEntity);
			var botCharacter = new BotCharacter
			{
				Skin = f.RNG->RandomElement(ctx.SkinOptions),
				DeathMarker = f.RNG->RandomElement(ctx.DeathMakers),
				Glider = f.RNG->RandomElement(ctx.Gliders),
				BotNameIndex = listNamesIndex,
				BehaviourType = config.BehaviourType,
				// We modify intervals to make them more unique to avoid performance spikes
				DecisionInterval = config.DecisionInterval,
				LookForTargetsToShootAtInterval = config.LookForTargetsToShootAtInterval,
				VisionRangeSqr = config.VisionRangeSqr,
				AccuracySpreadAngle = config.AccuracySpreadAngle,
				ChanceToUseSpecial = config.ChanceToUseSpecial,
				SpecialAimingDeviation = config.SpecialAimingDeviation,
				NextDecisionTime = FP._0,
				NextLookForTargetsToShootAtTime = FP._0,
				CurrentEvasionStepEndTime = FP._0,
				StuckDetectionPosition = FPVector3.Zero,
				LoadoutGearNumber = config.LoadoutGearNumber,
				LoadoutRarity = config.LoadoutRarity,
				MaxAimingRange = config.MaxAimingRange,
				MovementSpeedMultiplier = config.MovementSpeedMultiplier,
				TeamSize = ctx.TeamSize,
				MaxDistanceToTeammateSquared = config.MaxDistanceToTeammateSquared,
				DamageTakenMultiplier = config.DamageTakenMultiplier,
				DamageDoneMultiplier = config.DamageDoneMultiplier,
				SpeedResetAfterLanding = false,
				WanderDirection = f.RNG->NextBool(),
				InvalidMoveTargets = f.AllocateHashSet<EntityRef>(),
				TimeStartRunningFromCircle = f.RNG->NextInclusive(FP._2, FP._10 * FP._3)
			};

			ctx.BotNamesIndices.RemoveAt(listNamesIndex);

			// Remove a spawner from list when we took a new one for another team; update stored teamId
			if (ctx.PlayerSpawners.Count > 1)
			{
				ctx.PlayerSpawners.RemoveAt(rngSpawnIndex);
			}

			f.Add(botEntity, pathfinder); // Must be defined before the steering agent
			f.Add(botEntity, navMeshAgent);
			f.Add(botEntity, botCharacter);

			// Calculate bot trophies
			// TODO: Uncomment the old way of calculating trophies when we make Visual Trophies and Hidden Trophies
			// var trophies = (uint) ((botsDifficulty * botsTrophiesStep) + 1000 + f.RNG->Next(-50, 50));
			var trophies = (uint)Math.Max(0, ctx.AverageTrophies + f.RNG->Next(-50, 50));

			// Giving bots random weapon based on loadout rarity provided in bot configs
			var randomWeapon = new Equipment(f.RNG->RandomElement(ctx.WeaponsPool), EquipmentEdition.Genesis, botCharacter.LoadoutRarity);

			List<Modifier> modifiers = null;

			if (config.DamageDoneMultiplier != FP._1 || config.DamageTakenMultiplier != FP._1)
			{
				modifiers = new List<Modifier>();

				if (config.DamageTakenMultiplier != FP._1)
				{
					modifiers.Add(new Modifier
					{
						Id = ++f.Global->ModifierIdCount,
						Type = StatType.Armour,
						OpType = OperationType.Add,
						Power = FP._100 * (config.DamageTakenMultiplier - 1),
						Duration = FP.MaxValue,
						StartTime = FP._0,
						IsNegative = true
					});
				}

				if (config.DamageDoneMultiplier != FP._1)
				{
					modifiers.Add(new Modifier
					{
						Id = ++f.Global->ModifierIdCount,
						Type = StatType.Power,
						OpType = OperationType.Multiply,
						Power = FP._1 - config.DamageDoneMultiplier,
						Duration = FP.MaxValue,
						StartTime = FP._0,
						IsNegative = true
					});
				}
			}


			playerCharacter->Init(f, botEntity, id, spawnerTransform, 1, trophies, botCharacter.Skin,
				botCharacter.DeathMarker, botCharacter.Glider, teamId, Array.Empty<Equipment>(), randomWeapon, modifiers);
		}


		private static void AddBotTeams(BotSetupContext ctx)
		{
			// Add missing teams for the bots
			var currentTeams = ctx.PlayersByTeam.Count;
			for (var i = 0; i < ctx.TotalTeamsInGameMode - currentTeams; i++)
			{
				ctx.PlayersByTeam.Add(Constants.TEAM_ID_START_BOT_PARTIES + i, new List<EntityRef>());
			}
		}

		private Dictionary<int, List<EntityRef>> GetPlayersByTeam(Frame f)
		{
			var playersByTeam = new Dictionary<int, List<EntityRef>>();
			foreach (var player in f.Unsafe.GetComponentBlockIterator<PlayerCharacter>())
			{
				var teamId = player.Component->TeamId;
				if (teamId > 0)
				{
					if (!playersByTeam.TryGetValue(teamId, out var entities))
					{
						entities = new List<EntityRef>();
						playersByTeam[teamId] = entities;
					}

					entities.Add(player.Entity);
				}
			}

			return playersByTeam;
		}

		private int GetBotTeamId(Frame frame, PlayerRef bot, Dictionary<int, List<EntityRef>> playerByTeam)
		{
			if (!frame.Context.GameModeConfig.Teams)
			{
				return bot;
			}

			var @override = frame.Context.GameModeConfig.BotsTeamOverride;
			if (@override != 0)
			{
				return @override;
			}

			var maxPlayers = frame.Context.GameModeConfig.MaxPlayersInTeam;
			foreach (var kv in playerByTeam)
			{
				if (kv.Value.Count < maxPlayers)
				{
					return kv.Key;
				}
			}

			return bot;
		}

		private List<EntityComponentPointerPair<PlayerSpawner>> GetFreeSpawnPoints(Frame f)
		{
			var list = new List<EntityComponentPointerPair<PlayerSpawner>>();
			var entity = EntityRef.None;

			foreach (var pair in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (f.Time < pair.Component->ActivationTime)
				{
					entity = !entity.IsValid ||
						f.Get<PlayerSpawner>(entity).ActivationTime > pair.Component->ActivationTime
							? pair.Entity
							: entity;
					continue;
				}

				list.Add(pair);
			}

			if (list.Count == 0)
			{
				list.Add(new EntityComponentPointerPair<PlayerSpawner> { Component = f.Unsafe.GetPointer<PlayerSpawner>(entity), Entity = entity });
			}

			return list;
		}

		private int GetSpawnPointIndexByTypeOfBot(Frame f, List<EntityComponentPointerPair<PlayerSpawner>> spawnPoints, BotBehaviourType botType)
		{
			// Try to find spawners that are specific to the type of bot
			var specificSpawnPoints = new List<int>();
			for (int i = 0; i < spawnPoints.Count; i++)
			{
				var playerSpawner = spawnPoints[i].Component;
				if (playerSpawner->SpawnerType == SpawnerType.BotOfType && playerSpawner->BehaviourType == botType)
				{
					specificSpawnPoints.Add(i);
				}
			}

			if (specificSpawnPoints.Count > 0)
			{
				return specificSpawnPoints[f.RNG->Next(0, specificSpawnPoints.Count)];
			}

			return f.RNG->Next(0, spawnPoints.Count);
		}

		private List<QuantumBotConfig> GetBotConfigsList(Frame f, uint baseTrophiesAmount)
		{
			var botGamemodeKey = f.Context.GameModeConfig.UseBotsFromGamemode;
			if (botGamemodeKey == null || botGamemodeKey.Trim().Length == 0)
			{
				botGamemodeKey = f.Context.GameModeConfig.Id;
			}

			return GetBotConfigsFromTrophiesAmount(f, baseTrophiesAmount, botGamemodeKey);
		}


		private List<QuantumBotConfig> GetBotConfigsFromTrophiesAmount(Frame f, uint trophiesAmount, string botGamemodeKey)
		{
			// If there is no config it will return the default one;
			var matchedDifficulties = f.BotDifficultyConfigs.BotDifficulties.Where(bd => trophiesAmount >= bd.MinTrophies && trophiesAmount <= bd.MaxTrophies)
				.Select(bd => bd.BotDifficulty);
			var difficulties = matchedDifficulties.ToList();
			// If there is no matched config it will use 0, because it is uint default value
			var difficulty = difficulties.FirstOrDefault();
			var configs = f.BotConfigs.QuantumConfigs;
			return configs.Where(config => config.Difficulty == difficulty && config.GameMode == botGamemodeKey).ToList();
		}
	}
}