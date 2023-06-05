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

		private void AddBots(Frame f, List<PlayerRef> botIds, uint baseTrophiesAmount)
		{
			var teamSize = f.Context.GameModeConfig.Teams ? (int)f.Context.GameModeConfig.MaxPlayersInTeam : 1;
			var playerSpawners = GetFreeSpawnPoints(f);
			var botsNameCount = f.GameConfig.BotsNameCount;
			var botNamesIndices = new List<int>(botsNameCount);
			var deathMakers = GameIdGroup.DeathMarker.GetIds();
			var gliders = GameIdGroup.Glider.GetIds();
			var botItems = GameIdGroup.BotItem.GetIds();
			var skinOptions = GameIdGroup.PlayerSkin.GetIds().Where(item => botItems.Contains(item)).ToArray();
			var weaponsPool = new List<GameId>(GameIdGroup.Weapon.GetIds());
			weaponsPool.Remove(GameId.Hammer);

			var forcedBotTypes = new List<BotBehaviourType>();
			foreach (var playerSpawner in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				if (playerSpawner.Component->SpawnerType == SpawnerType.BotOfType)
				{
					forcedBotTypes.Add(playerSpawner.Component->BehaviourType);
				}
			}

			var botConfigsList = GetBotConfigsList(f, baseTrophiesAmount);
			if (botConfigsList.Count == 0)
			{
				throw new Exception("Bot configs not found for this game!");
			}


			for (var i = 0; i < botsNameCount; i++)
			{
				botNamesIndices.Add(i + 1);
			}

			var playerCharacterPrototypeAsset =
				f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id);
			var navMeshAgentConfig = f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id);


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

			// Add missing teams for the bots
			uint totalTeams = f.Context.GameModeConfig.MaxPlayers / (f.Context.GameModeConfig.Teams ? f.Context.GameModeConfig.MaxPlayersInTeam : 1);
			int currentTeams = playersByTeam.Count;
			for (int i = 0; i < totalTeams - currentTeams; i++)
			{
				playersByTeam.Add(Constants.TEAM_ID_START_BOT_PARTIES + i, new List<EntityRef>());
			}

			byte botIndex = 0;
			foreach (var id in botIds)
			{
				var teamId = this.GetBotTeamId(f, id, playersByTeam);

				var rngBotConfigIndex = f.RNG->Next(0, botConfigsList.Count);
				var botConfig = botConfigsList[rngBotConfigIndex];

				// If there are spawns for specific types of bots, we use those
				var forcedTypeConfigIndex = -1;
				while (forcedBotTypes.Count > 0 && forcedTypeConfigIndex == -1)
				{
					forcedTypeConfigIndex = botConfigsList.FindIndex(config => config.BehaviourType == forcedBotTypes[0]);
					forcedBotTypes.RemoveAt(0);
				}

				if (forcedTypeConfigIndex > -1)
				{
					botConfig = botConfigsList[forcedTypeConfigIndex];
				}

				var withPlayer = false;
				Transform3D spawnerTransform;
				var rngSpawnIndex = 0;
				if (playersByTeam.TryGetValue(teamId, out var teamMembers) && teamMembers.Count > 0)
				{
					spawnerTransform = f.Get<Transform3D>(teamMembers.First());
					spawnerTransform.Position.Y -= FP._2;
					withPlayer = true;
				}
				else
				{
					rngSpawnIndex = GetSpawnPointIndexByTypeOfBot(f, playerSpawners, botConfig.BehaviourType);
					spawnerTransform = f.Get<Transform3D>(playerSpawners[rngSpawnIndex].Entity);
				}


				var botEntity = f.Create(playerCharacterPrototypeAsset);
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
				var navMeshAgent = new NavMeshSteeringAgent();
				var pathfinder = NavMeshPathfinder.Create(f, botEntity, navMeshAgentConfig);
				var listNamesIndex = f.RNG->Next(0, botNamesIndices.Count);

				if (!playersByTeam.TryGetValue(teamId, out var entities))
				{
					entities = new List<EntityRef>();
					playersByTeam[teamId] = entities;
				}

				entities.Add(botEntity);
				var botCharacter = new BotCharacter
				{
					BotIndex = botIndex++,
					Skin = skinOptions[f.RNG->Next(0, skinOptions.Length)],
					DeathMarker = deathMakers[f.RNG->Next(0, deathMakers.Count)],
					Glider = gliders[f.RNG->Next(0, deathMakers.Count)],
					BotNameIndex = botNamesIndices[listNamesIndex],
					BehaviourType = botConfig.BehaviourType,
					// We modify intervals to make them more unique to avoid performance spikes
					DecisionInterval = botConfig.DecisionInterval +
						botNamesIndices[listNamesIndex] * FP._0_01 * FP._0_10,
					LookForTargetsToShootAtInterval = botConfig.LookForTargetsToShootAtInterval +
						botNamesIndices[listNamesIndex] * FP._0_01 * FP._0_01,
					VisionRangeSqr = botConfig.VisionRangeSqr,
					AccuracySpreadAngle = botConfig.AccuracySpreadAngle,
					ChanceToUseSpecial = botConfig.ChanceToUseSpecial,
					SpecialAimingDeviation = botConfig.SpecialAimingDeviation,
					NextDecisionTime = FP._0,
					NextLookForTargetsToShootAtTime = FP._0,
					CurrentEvasionStepEndTime = FP._0,
					StuckDetectionPosition = FPVector3.Zero,
					LoadoutGearNumber = botConfig.LoadoutGearNumber,
					LoadoutRarity = botConfig.LoadoutRarity,
					MaxAimingRange = botConfig.MaxAimingRange,
					MovementSpeedMultiplier = botConfig.MovementSpeedMultiplier,
					TeamSize = teamSize,
					MaxDistanceToTeammateSquared = botConfig.MaxDistanceToTeammateSquared,
					SpawnWithPlayer = withPlayer,
					DamageTakenMultiplier = botConfig.DamageTakenMultiplier,
					DamageDoneMultiplier = botConfig.DamageDoneMultiplier,
					SpeedResetAfterLanding = false,
					WanderDirection = f.RNG->Next() > FP._0_50,
					InvalidMoveTargets = f.AllocateHashSet<EntityRef>(),
					TimeStartRunningFromCircle = f.RNG->NextInclusive(FP._2, FP._10 * FP._3)
				};

				botNamesIndices.RemoveAt(listNamesIndex);

				// Remove a spawner from list when we took a new one for another team; update stored teamId
				if (!withPlayer && playerSpawners.Count > 1)
				{
					playerSpawners.RemoveAt(rngSpawnIndex);
				}

				f.Add(botEntity, pathfinder); // Must be defined before the steering agent
				f.Add(botEntity, navMeshAgent);
				f.Add(botEntity, botCharacter);

				// Calculate bot trophies
				// TODO: Uncomment the old way of calculating trophies when we make Visual Trophies and Hidden Trophies
				// var trophies = (uint) ((botsDifficulty * botsTrophiesStep) + 1000 + f.RNG->Next(-50, 50));
				var trophies = (uint)Math.Max(0, baseTrophiesAmount + f.RNG->Next(-50, 50));

				// Giving bots random weapon based on loadout rarity provided in bot configs
				var randomWeapon = new Equipment(weaponsPool[f.RNG->Next(0, weaponsPool.Count)],
					EquipmentEdition.Genesis, botCharacter.LoadoutRarity);

				List<Modifier> modifiers = null;

				if (botConfig.DamageDoneMultiplier != FP._1 || botConfig.DamageTakenMultiplier != FP._1)
				{
					modifiers = new List<Modifier>();

					if (botConfig.DamageTakenMultiplier != FP._1)
					{
						modifiers.Add(new Modifier
						{
							Id = ++f.Global->ModifierIdCount,
							Type = StatType.Armour,
							OpType = OperationType.Add,
							Power = FP._100 * (botConfig.DamageTakenMultiplier - 1),
							Duration = FP.MaxValue,
							StartTime = FP._0,
							IsNegative = true
						});
					}

					if (botConfig.DamageDoneMultiplier != FP._1)
					{
						modifiers.Add(new Modifier
						{
							Id = ++f.Global->ModifierIdCount,
							Type = StatType.Power,
							OpType = OperationType.Multiply,
							Power = FP._1 - botConfig.DamageDoneMultiplier,
							Duration = FP.MaxValue,
							StartTime = FP._0,
							IsNegative = true
						});
					}
				}


				playerCharacter->Init(f, botEntity, id, spawnerTransform, 1, trophies, botCharacter.Skin,
					botCharacter.DeathMarker, botCharacter.Glider, teamId, Array.Empty<Equipment>(), randomWeapon, modifiers);
			}
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