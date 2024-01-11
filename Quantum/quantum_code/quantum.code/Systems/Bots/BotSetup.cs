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
			public List<EntityComponentPointerPair<PlayerSpawner>> AllSpawners;
			public List<EntityComponentPointerPair<PlayerSpawner>> AvailableSpawners;
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
			if (f.ComponentCount<BotCharacter>() > 0)
			{
				return;
			}

			AddBotBehaviourToPlayers(f, baseTrophiesAmount);

			if (!f.Context.GameModeConfig.AllowBots)
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
			var spawners = GetFreeSpawnPoints(f);
			var ctx = new BotSetupContext()
			{
				TeamSize = f.Context.GameModeConfig.Teams ? (int)f.Context.GameModeConfig.MaxPlayersInTeam : 1,
				AvailableSpawners = spawners.ToList(),
				AllSpawners = spawners.ToList(),
				BotNamesIndices = Enumerable.Range(1, f.GameConfig.BotsNameCount).ToList(),
				DeathMakers = GameIdGroup.DeathMarker.GetIds(),
				Gliders = GameIdGroup.Glider.GetIds(),
				SkinOptions = GameIdGroup.PlayerSkin.GetIds().Where(item => botItems.Contains(item)).ToArray(),
				WeaponsPool = GameIdGroup.Weapon.GetIds().Where(id => id != GameId.Hammer
					&& !id.IsInGroup(GameIdGroup.Deprecated)).ToList(),
				BotConfigs = GetBotConfigsList(f, baseTrophies),
				AverageTrophies = baseTrophies,
				PlayerPrototype = f.FindAsset<EntityPrototype>(f.AssetConfigs.PlayerCharacterPrototype.Id),
				NavMeshAgentConfig = f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id),
				PlayersByTeam = TeamHelpers.GetPlayersByTeam(f),
				TotalTeamsInGameMode = f.Context.GameModeConfig.MaxPlayers /
					(f.Context.GameModeConfig.Teams ? f.Context.GameModeConfig.MaxPlayersInTeam : 1)
			};
			AddBotTeams(ctx);
			return ctx;
		}

		private void AddBotBehaviourToPlayers(Frame f, uint baseTrophies)

		{
			var playersUsingBotBehaviour = Enumerable.Range(0, f.PlayerCount)
				.Where(i => (f.GetPlayerInputFlags(i) & DeterministicInputFlags.PlayerNotPresent) == 0)
				.Select(i => (playerRef: i, data: f.GetPlayerData(i)))
				.Where(player => player.data != null && player.data.UseBotBehaviour)
				.Select(player => player.playerRef).ToList();

			if (playersUsingBotBehaviour.Count == 0)
			{
				return;
			}

			var ctx = GetBotContext(f, baseTrophies);
			if (ctx.BotConfigs.Count == 0)
			{
				throw new Exception("Bot configs not found for this game!");
			}

			foreach (var playerRef in playersUsingBotBehaviour)
			{
				var botConfig = f.RNG->RandomElement(ctx.BotConfigs);

				AddBot(f, ctx, playerRef, botConfig, true);
			}
		}


		private void AddBots(Frame f, List<PlayerRef> playerRefs, uint baseTrophies)

		{
			var ctx = GetBotContext(f, baseTrophies);
			if (ctx.BotConfigs.Count == 0)
			{
				throw new Exception("Bot configs not found for this game!");
			}

			else
			{
				BotLogger.LogAction(EntityRef.None,
					$"Using configs levels {string.Join(",", ctx.BotConfigs.Select(c => c.Difficulty))}");
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
					forcedTypeConfigIndex =
						ctx.BotConfigs.FindIndex(config => config.BehaviourType == forcedBotTypes[0]);
					forcedBotTypes.RemoveAt(0);
				}

				if (forcedTypeConfigIndex > -1)
				{
					botConfig = ctx.BotConfigs[forcedTypeConfigIndex];
				}


				AddBot(f, ctx, playerRef, botConfig);
			}
		}

		private void AddBot(Frame f, BotSetupContext ctx, PlayerRef id, QuantumBotConfig config,
							bool realPlayer = false)
		{
			var teamId = GetBotTeamId(f, id, ctx.PlayersByTeam);

			var botEntity = realPlayer
				? f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[id].Entity
				: f.Create(ctx.PlayerPrototype);
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
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
				DecisionInterval = config.DecisionInterval,
				LookForTargetsToShootAtInterval = config.LookForTargetsToShootAtInterval,
				VisionRangeSqr = config.VisionRangeSqr,
				AccuracySpreadAngle = config.AccuracySpreadAngle,
				ChanceToUseSpecial = config.ChanceToUseSpecial,
				SpecialAimingDeviation = config.SpecialAimingDeviation,
				LoadoutGearNumber = config.LoadoutGearNumber,
				LoadoutRarity = config.LoadoutRarity,
				MaxAimingRange = config.MaxAimingRange,
				MovementSpeedMultiplier = config.MovementSpeedMultiplier,
				TeamSize = ctx.TeamSize,
				MaxDistanceToTeammateSquared = config.MaxDistanceToTeammateSquared,
				DamageTakenMultiplier = config.DamageTakenMultiplier,
				DamageDoneMultiplier = config.DamageDoneMultiplier,
				SpeedResetAfterLanding = false,
				TimeStartRunningFromCircle = f.RNG->NextInclusive(config.MinRunFromZoneTime, config.MaxRunFromZoneTime),
				SpecialCooldown = new FPVector2(config.MinSpecialCooldown, config.MaxSpecialCooldown),

				// state
				WanderDirection = f.RNG->NextBool(),
				InvalidMoveTargets = f.AllocateHashSet<EntityRef>(),
				NextDecisionTime = FP._0,
				NextLookForTargetsToShootAtTime = FP._0,
				NextAllowedSpecialUseTime = FP._0,
				StuckDetectionPosition = FPVector2.Zero,
			};

			ctx.BotNamesIndices.Remove(listNamesIndex);


			f.Add(botEntity, pathfinder); // Must be defined before the steering agent
			f.Add(botEntity, new NavMeshSteeringAgent());
			f.Add(botEntity, botCharacter);

			if (realPlayer) return; // wtf ?

			// Calculate bot trophies
			// TODO: Uncomment the old way of calculating trophies when we make Visual Trophies and Hidden Trophies
			// var trophies = (uint) ((botsDifficulty * botsTrophiesStep) + 1000 + f.RNG->Next(-50, 50));
			var trophies = (uint)Math.Max(0, ctx.AverageTrophies + f.RNG->Next(-50, 50));

			// Giving bots random weapon based on loadout rarity provided in bot configs
			var randomWeapon = new Equipment(f.RNG->RandomElement(ctx.WeaponsPool), EquipmentEdition.Genesis,
				botCharacter.LoadoutRarity);

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


			EntityComponentPointerPair<PlayerSpawner> spawner;
			if (ctx.AvailableSpawners.Count > 0)
			{
				var rngSpawnIndex = GetSpawnPointForBot(f, ctx, config, teamId);
				spawner = ctx.AvailableSpawners[rngSpawnIndex];
				ctx.AvailableSpawners.RemoveAt(rngSpawnIndex);
			}
			else
			{
				// If we don't have an available spawner get a random one from all
				spawner = f.RNG->RandomElement(ctx.AllSpawners);
			}

			var spawnerTransform = f.Get<Transform3D>(spawner.Entity);


			var kccConfig = f.FindAsset<CharacterController3DConfig>(f.AssetConfigs.BotKccConfig.Id);
			var setup = new PlayerCharacterSetup()
			{
				e = botEntity,
				playerRef = id,
				spawnPosition = spawnerTransform,
				playerLevel = 1,
				trophies = trophies,
				teamId = teamId,
				modifiers = modifiers,
				KccConfig = kccConfig
			};
			
			SetupBotSkins(f, botEntity);
			playerCharacter->Init(f, setup);
			
			if (f.Unsafe.TryGetPointer<BotLoadout>(spawner.Entity, out var botLoadout))
			{
				playerCharacter->AddWeapon(f, botEntity, ref botLoadout->Weapon, true);
				playerCharacter->EquipSlotWeapon(f, botEntity, Constants.WEAPON_INDEX_PRIMARY);
			}
			
			if (GetAmmoPercentage(f, ref spawner, out var percentage))
			{
				f.Unsafe.GetPointer<Stats>(botEntity)->SetCurrentAmmo(f, playerCharacter, botEntity, percentage);
			}
		}

		private GameId RandomBotCosmeticInGroup(Frame f, GameIdGroup group)
		{
			var availableSkins = group.GetIds().Where(a => a.IsInGroup(GameIdGroup.BotItem)).ToArray();
			return f.RNG->RandomElement(availableSkins);
		}

		private void SetupBotSkins(Frame f, EntityRef entity)
		{
			f.Add<CosmeticsHolder>(entity);
			f.Unsafe.GetPointer<CosmeticsHolder>(entity)->SetCosmetics(f, new[]
			{
				RandomBotCosmeticInGroup(f, GameIdGroup.PlayerSkin),
				RandomBotCosmeticInGroup(f, GameIdGroup.MeleeSkin),
				RandomBotCosmeticInGroup(f, GameIdGroup.Glider),
				RandomBotCosmeticInGroup(f, GameIdGroup.DeathMarker),
			});
		}

		private bool GetAmmoPercentage(Frame f, ref EntityComponentPointerPair<PlayerSpawner> spawner,
									   out FP percentage)
		{
			percentage = FP._0;
			return false;
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
				list.Add(new EntityComponentPointerPair<PlayerSpawner>
					{ Component = f.Unsafe.GetPointer<PlayerSpawner>(entity), Entity = entity });
			}

			return list;
		}

		private int GetSpawnPointForBot(Frame f, BotSetupContext ctx, QuantumBotConfig botConfig, int teamId)
		{
			if (GetSpecificSpawn(f, ctx, botConfig,SpawnerType.BotOfType, out var specificSpawnPoint)) return specificSpawnPoint;
			if (GetSpecificSpawn(f, ctx, botConfig,SpawnerType.AnyBot, out var anyBotSpawn)) return anyBotSpawn;

			if (GetSpawnClosestToTeam(f, ctx, teamId, out var spawnPointForBot)) return spawnPointForBot;

			// Otherwise try to put bot at random
			return f.RNG->Next(0, ctx.AvailableSpawners.Count);
		}


		private bool GetSpecificSpawn(Frame f, BotSetupContext ctx, QuantumBotConfig botConfig, SpawnerType type,
									  out int specificSpawnPoint)
		{
			// Try to find spawners that are specific to the type of bot
			var botType = botConfig.BehaviourType;
			var specificSpawnPoints = new List<int>();
			for (var i = 0; i < ctx.AvailableSpawners.Count; i++)
			{
				var playerSpawner = ctx.AvailableSpawners[i].Component;
				if (type == SpawnerType.BotOfType && playerSpawner->SpawnerType == SpawnerType.BotOfType && playerSpawner->BehaviourType == botType)
				{
					specificSpawnPoints.Add(i);
				}

				if (type == SpawnerType.AnyBot && playerSpawner->SpawnerType == SpawnerType.AnyBot)
				{
					specificSpawnPoints.Add(i);
				}
			}

			if (specificSpawnPoints.Count > 0)
			{
				specificSpawnPoint = f.RNG->RandomElement(specificSpawnPoints);
				return true;
			}

			specificSpawnPoint = -1;
			return false;
		}
		

		private bool GetSpawnClosestToTeam(Frame f, BotSetupContext ctx, int teamId, out int spawnPointForBot)
		{
			if (ctx.TeamSize <= 1)
			{
				spawnPointForBot = 0;
				return false;
			}

			// Get players in bot team and this point the bot is not in this list
			if (ctx.PlayersByTeam.TryGetValue(teamId, out var players) && players.Count > 0)
			{
				var randomPlayer = EntityRef.None;

				// We are trying to find an actual real player in a team
				for (var i = 0; i < players.Count; i++)
				{
					if (f.Has<BotCharacter>(players[i]))
					{
						continue;
					}

					randomPlayer = players[i];

					break;
				}

				// If we didn't find a real player in a team then we look for a bot with defined spawn position (via transform3d)
				if (randomPlayer == EntityRef.None)
				{
					for (var i = 0; i < players.Count; i++)
					{
						if (f.Unsafe.TryGetPointer<Transform3D>(players[i], out var pt) &&
							pt->Position != FPVector3.Zero)
						{
							randomPlayer = players[i];
							break;
						}
					}
				}

				// If we still have no one then we return and allow other logic to choose a random free spawn point
				if (randomPlayer == EntityRef.None)
				{
					spawnPointForBot = 0;
					return false;
				}

				// If we DID find a real player or bot with defined position then we look for a spot to spawn nearby
				if (f.TryGet<Transform3D>(randomPlayer, out var transform))
				{
					var position = transform.Position.XZ;

					// Get closest
					var closestIndex = -1;
					var closestDistance = FP.MaxValue;

					for (var i = 0; i < ctx.AvailableSpawners.Count; i++)
					{
						var spawnerPosition = f.Get<Transform3D>(ctx.AvailableSpawners[i].Entity).Position.XZ;

						var distance = FPVector2.DistanceSquared(position, spawnerPosition);
						if (distance < closestDistance)
						{
							closestIndex = i;
							closestDistance = distance;
						}
					}

					if (closestIndex != -1)
					{
						spawnPointForBot = closestIndex;
						return true;
					}
				}
			}

			spawnPointForBot = 0;
			return false;
		}


		private List<QuantumBotConfig> GetBotConfigsList(Frame f, uint baseTrophiesAmount)
		{
			var botGamemodeKey = f.Context.GameModeConfig.UseBotsFromGamemode;
			if (botGamemodeKey == null || botGamemodeKey.Trim().Length == 0)
			{
				botGamemodeKey = f.Context.GameModeConfig.Id;
			}

			if (f.RuntimeConfig.BotOverwriteDifficulty != -1)
			{
				BotLogger.LogAction(EntityRef.None,
					"Using config difficulty " + f.RuntimeConfig.BotOverwriteDifficulty);
				var configs = f.BotConfigs.QuantumConfigs;
				return configs.Where(config =>
						config.Difficulty == f.RuntimeConfig.BotOverwriteDifficulty &&
						config.GameMode == botGamemodeKey)
					.ToList();
			}

			var trophiesConfigs = GetBotConfigsFromTrophiesAmount(f, baseTrophiesAmount, botGamemodeKey);
			if (trophiesConfigs.Count > 0)
			{
				return trophiesConfigs;
			}

			// Uses configs from gamemode without difficulty
			return f.BotConfigs.QuantumConfigs.Where(config => config.GameMode == botGamemodeKey).ToList();
		}


		private List<QuantumBotConfig> GetBotConfigsFromTrophiesAmount(Frame f, uint trophiesAmount,
																	   string botGamemodeKey)
		{
			// If there is no config it will return the default one;
			var matchedDifficulties = f.BotDifficultyConfigs.BotDifficulties.Where(bd =>
					trophiesAmount >= bd.MinTrophies && trophiesAmount <= bd.MaxTrophies)
				.Select(bd => bd.BotDifficulty);
			var difficulties = matchedDifficulties.ToList();
			// If there is no matched config it will use 0, because it is uint default value
			var difficulty = difficulties.FirstOrDefault();
			var configs = f.BotConfigs.QuantumConfigs;
			return configs.Where(config => config.Difficulty == difficulty && config.GameMode == botGamemodeKey)
				.ToList();
		}
	}
}