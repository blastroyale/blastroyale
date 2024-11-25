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
		public static readonly string FasterIntervalBotConfig = "smarterbot";

		private static readonly WeightedList<GameId> PreferredWeapons = new WeightedList<GameId>()
		{
			{ GameId.ApoMinigun, 3 },
			{ GameId.ModMachineGun, 1 },
			{ GameId.ModShotgun, 3 },
			{ GameId.ModRifle, 2 },
			{ GameId.GunARBurst, 2 },
			{ GameId.ModLauncher, 3 },
			{ GameId.ModPistol, 1 },
			{ GameId.ModSniper, 3 },
			{ GameId.GunSniperHeavy, 2 },
		};

		private class BotSetupContext
		{
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

			AddFasterBotAgent(f, f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id));
			AddBotBehaviourToPlayers(f, baseTrophiesAmount);

			if (!f.Context.GameModeConfig.AllowBots)
			{
				return;
			}

			var playerLimit = f.PlayerCount;
			var botIds = new List<PlayerRef>();
			var maxPlayers = f.Context.MapConfig.MaxPlayers;
			if (playerLimit == 1 && maxPlayers > 1) // offline game with bots
			{
				playerLimit = (int)maxPlayers;
			}

			for (var i = 0; i < playerLimit; i++)
			{
				if (i >= f.PlayerCount || (f.GetPlayerInputFlags(i) & DeterministicInputFlags.PlayerNotPresent) ==
					DeterministicInputFlags.PlayerNotPresent || f.GetPlayerData(i) == null)
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
			var navigation = f.FindAsset<NavMeshAgentConfig>(f.AssetConfigs.BotNavMeshConfig.Id);
			var ctx = new BotSetupContext()
			{
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
				NavMeshAgentConfig = navigation,
				PlayersByTeam = TeamSystem.GetPlayersByTeam(f),
				TotalTeamsInGameMode = (uint)f.PlayerCount / f.GetTeamSize()
			};
			AddBotTeams(ctx);
			return ctx;
		}

		private void AddFasterBotAgent(Frame f, NavMeshAgentConfig c)
		{
			var newValue = new NavMeshAgentConfig()
			{
				Path = FasterIntervalBotConfig,
				Acceleration = c.Acceleration,
				MovementType = c.MovementType,
				Priority = c.Priority,
				Speed = c.Speed,
				AvoidanceQuality = c.AvoidanceQuality,
				PathQuality = c.PathQuality,
				AvoidanceType = c.AvoidanceType,
				AngularSpeed = c.AngularSpeed,
				CachedWaypointCount = c.CachedWaypointCount,
				AutoBraking = c.AutoBraking,
				AvoidanceLayer = c.AvoidanceLayer,
				AvoidanceMask = c.AvoidanceMask,
				AvoidanceRadius = c.AvoidanceRadius,
				StoppingDistance = c.StoppingDistance,
				UpdateInterval = 5,
				VerticalPositioning = c.VerticalPositioning,
				AutoBrakingDistance = c.AutoBrakingDistance,
				ReduceAvoidanceFactor = c.ReduceAvoidanceFactor,
				AutomaticTargetCorrection = c.AutomaticTargetCorrection,
				AutomaticTargetCorrectionRadius = c.AutomaticTargetCorrectionRadius,
				ShowDebugSteering = c.ShowDebugSteering,
				ShowDebugAvoidance = c.ShowDebugAvoidance,
				MaxAvoidanceCandidates = c.MaxAvoidanceCandidates,
				MaxRepathTimeout = c.MaxRepathTimeout,
				AvoidanceCanReduceSpeed = c.AvoidanceCanReduceSpeed,
				ClampAgentToNavmesh = c.ClampAgentToNavmesh,
				DynamicLineOfSight = c.DynamicLineOfSight,
				EnableWaypointDetectionAxis = c.EnableWaypointDetectionAxis,
				DefaultWaypointDetectionDistance = c.DefaultWaypointDetectionDistance,
				LineOfSightFunneling = c.LineOfSightFunneling,
				ReduceAvoidanceAtWaypoints = c.ReduceAvoidanceAtWaypoints,
				WaypointDetectionAxisExtend = c.WaypointDetectionAxisExtend,
				ClampAgentToNavmeshCorrection = c.ClampAgentToNavmeshCorrection,
				WaypointDetectionAxisOffset = c.WaypointDetectionAxisOffset,
				DynamicLineOfSightWaypointRange = c.DynamicLineOfSightWaypointRange,
			};
			f.DynamicAssetDB.AddAsset(newValue);
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

				AddBot(f, ctx, playerRef, ref botConfig, true);
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
				BotLogger.LogAction(f, EntityRef.None,
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


				AddBot(f, ctx, playerRef, ref botConfig);
			}
		}

		private void AddBot(Frame f, BotSetupContext ctx, PlayerRef id, ref QuantumBotConfig config,
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
				TeamSize = (int)f.GetTeamSize(),
				MovementType = BotMovementType.None,
				FavoriteWeapon = PreferredWeapons.Next(f),
				WillFightInZone = f.RNG->Next() < FP._0_33
			};

			ctx.BotNamesIndices.Remove(listNamesIndex);


			f.Add(botEntity, pathfinder); // Must be defined before the steering agent
			f.Add(botEntity, new NavMeshSteeringAgent());
			f.Add(botEntity, new NavMeshAvoidanceAgent());
			f.Add(botEntity, botCharacter);
			// We can toggle a flag in the editor to add bot behaviour to the local player, also used in automated tests
			if (realPlayer) return;

			// Calculate bot trophies
			// TODO: Uncomment the old way of calculating trophies when we make Visual Trophies and Hidden Trophies
			// var trophies = (uint) ((botsDifficulty * botsTrophiesStep) + 1000 + f.RNG->Next(-50, 50));
			var trophies = (uint)Math.Max(0, ctx.AverageTrophies + f.RNG->Next(-50, 50));

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
				var rngSpawnIndex = GetSpawnPointForBot(f, ctx, ref config, teamId);
				spawner = ctx.AvailableSpawners[rngSpawnIndex];
				ctx.AvailableSpawners.RemoveAt(rngSpawnIndex);
			}
			else
			{
				// If we don't have an available spawner get a random one from all
				spawner = f.RNG->RandomElement(ctx.AllSpawners);
			}

			var spawnerTransform = f.Get<Transform2D>(spawner.Entity);
			var setup = new PlayerCharacterSetup()
			{
				e = botEntity,
				playerRef = id,
				spawnPosition = spawnerTransform,
				playerLevel = 1,
				trophies = trophies,
				teamId = teamId,
				modifiers = modifiers,
				deathFlagID = botCharacter.DeathMarker
			};

			SetupBotCosmetics(f, botEntity, spawner.Entity);
			playerCharacter->Init(f, setup);

			CheckUpdateTutorialRuntimeData(f, spawner.Entity, botEntity);
			if (f.Unsafe.TryGetPointer<BotLoadout>(spawner.Entity, out var botLoadout))
			{
				playerCharacter->AddWeapon(f, botEntity, botLoadout->Weapon, true);
				playerCharacter->EquipSlotWeapon(f, botEntity, Constants.WEAPON_INDEX_PRIMARY);
			}

			var aim = spawnerTransform.Rotation.ToDirection();
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botEntity);
			var kcc = f.Unsafe.GetPointer<TopDownController>(botEntity);
			kcc->AimDirection = aim;

			bb->Set(f, Constants.AIM_DIRECTION_KEY, aim);

			f.Destroy(spawner.Entity);
		}

		/// <summary>
		/// This is a hack because the spawner entity is not the same as the player who it spawns, so i change the tutorial runtime refs to the spawned player
		/// </summary>
		private void CheckUpdateTutorialRuntimeData(Frame f, EntityRef spawnerEntity, EntityRef botEntity)
		{
			if (!f.Unsafe.TryGetPointerSingleton<TutorialRuntimeData>(out var tutorialData))
			{
				return;
			}

			UpdateBotEntityIfMatch(ref tutorialData->FinalBot, spawnerEntity, botEntity);
			UpdateBotEntityIfMatch(ref tutorialData->GrenadeBot, spawnerEntity, botEntity);

			for (var i = 0; i < tutorialData->FirstBots.Length; i++)
			{
				UpdateBotEntityIfMatch(ref tutorialData->FirstBots[i], spawnerEntity, botEntity);
			}
		}

		private void UpdateBotEntityIfMatch(ref EntityRef targetEntity, EntityRef spawnerEntity, EntityRef botEntity)
		{
			if (targetEntity == spawnerEntity)
			{
				targetEntity = botEntity;
			}
		}

		private GameId RandomBotCosmeticInGroup(Frame f, GameIdGroup group)
		{
			var availableSkins = group.GetIds().Where(a => a.IsInGroup(GameIdGroup.BotItem)).ToArray();
			return f.RNG->RandomElement(availableSkins);
		}

		private void SetupBotCosmetics(Frame f, EntityRef entity, EntityRef spawnerEntity)
		{
			f.Add<CosmeticsHolder>(entity);
			if (f.Unsafe.TryGetPointer<CosmeticsHolder>(spawnerEntity, out var cosmetics))
			{
				var botCosmetics = f.ResolveList(cosmetics->Cosmetics).ToArray();
				f.Unsafe.GetPointer<CosmeticsHolder>(entity)->SetCosmetics(f, botCosmetics);
				return;
			}

			f.Unsafe.GetPointer<CosmeticsHolder>(entity)->SetCosmetics(f, new[]
			{
				RandomBotCosmeticInGroup(f, GameIdGroup.PlayerSkin),
				RandomBotCosmeticInGroup(f, GameIdGroup.MeleeSkin),
				RandomBotCosmeticInGroup(f, GameIdGroup.Glider),
				RandomBotCosmeticInGroup(f, GameIdGroup.DeathMarker),
			});
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
			if (frame.GetTeamSize() == 1)
			{
				return Constants.TEAM_ID_START_BOT_PARTIES + bot;
			}

			var @override = frame.Context.GameModeConfig.BotsTeamOverride;
			if (@override != 0)
			{
				return @override;
			}

			var maxPlayers = frame.RuntimeConfig.MatchConfigs.TeamSize;
			foreach (var kv in playerByTeam)
			{
				if (kv.Value.Count < maxPlayers)
				{
					return kv.Key;
				}
			}

			return Constants.TEAM_ID_START_BOT_PARTIES + bot;
		}

		private List<EntityComponentPointerPair<PlayerSpawner>> GetFreeSpawnPoints(Frame f)
		{
			var list = new List<EntityComponentPointerPair<PlayerSpawner>>();
			var entity = EntityRef.None;

			foreach (var pair in f.Unsafe.GetComponentBlockIterator<PlayerSpawner>())
			{
				list.Add(pair);
			}

			if (list.Count == 0)
			{
				list.Add(new EntityComponentPointerPair<PlayerSpawner>
					{ Component = f.Unsafe.GetPointer<PlayerSpawner>(entity), Entity = entity });
			}

			return list;
		}

		private int GetSpawnPointForBot(Frame f, BotSetupContext ctx, ref QuantumBotConfig botConfig, int teamId)
		{
			if (GetSpecificSpawn(f, ctx, ref botConfig, SpawnerType.BotOfType, out var specificSpawnPoint))
				return specificSpawnPoint;
			if (GetSpecificSpawn(f, ctx, ref botConfig, SpawnerType.AnyBot, out var anyBotSpawn)) return anyBotSpawn;

			if (GetSpawnClosestToTeam(f, ctx, teamId, out var spawnPointForBot)) return spawnPointForBot;

			// Otherwise try to put bot at random
			return f.RNG->Next(0, ctx.AvailableSpawners.Count);
		}


		private bool GetSpecificSpawn(Frame f, BotSetupContext ctx, ref QuantumBotConfig botConfig, SpawnerType type,
									  out int specificSpawnPoint)
		{
			// Try to find spawners that are specific to the type of bot
			var botType = botConfig.BehaviourType;
			var specificSpawnPoints = new List<int>();
			for (var i = 0; i < ctx.AvailableSpawners.Count; i++)
			{
				var playerSpawner = ctx.AvailableSpawners[i].Component;
				if (type == SpawnerType.BotOfType && playerSpawner->SpawnerType == SpawnerType.BotOfType &&
					playerSpawner->BehaviourType == botType)
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
			if (f.GetTeamSize() <= 1)
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
						if (f.Unsafe.TryGetPointer<Transform2D>(players[i], out var pt) &&
							pt->Position != FPVector2.Zero)
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
				if (f.TryGet<Transform2D>(randomPlayer, out var transform))
				{
					var position = transform.Position;

					// Get closest
					var closestIndex = -1;
					var closestDistance = FP.MaxValue;

					for (var i = 0; i < ctx.AvailableSpawners.Count; i++)
					{
						var spawnerPosition = f.Get<Transform2D>(ctx.AvailableSpawners[i].Entity).Position;

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

			if (f.RuntimeConfig.MatchConfigs.BotOverwriteDifficulty != -1)
			{
				BotLogger.LogAction(f, EntityRef.None,
					"Using config difficulty " + f.RuntimeConfig.MatchConfigs.BotOverwriteDifficulty);
				var configs = f.BotConfigs.QuantumConfigs;
				return configs.Where(config =>
						config.Difficulty == f.RuntimeConfig.MatchConfigs.BotOverwriteDifficulty &&
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