#if !DISABLE_SRDEBUGGER
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using Photon.Deterministic;
using Quantum;
using SRDebugger;

public partial class SROptions
{
	public enum EventModifier
	{
		DoubleXp,
		DoubleBpp,
		DoubleTrophies,
		DoubleNoobInGame,
		DrpNoobKill,
		DrpCoinAir,
		DrpBBChest,
		DrpNoobKill50,
		DrpCoinAir50,
		DrpBBChest50,
	}

	private static HashSet<EventModifier> _toggledModifiers = new ();
	private static HashSet<MutatorType> _mutators = new ();

	// Default Value for property
	private int _eventDuration = 30;
	private int _startsIn = 1;

	// Options will be grouped by category
	// Options will be grouped by category
	[Category("Events")]
	[Sort(10)]
	public int StartsIn
	{
		get { return _startsIn; }
		set { _startsIn = value; }
	}

	[Category("Events")]
	[Sort(11)]
	public int EventDuration
	{
		get { return _eventDuration; }
		set { _eventDuration = value; }
	}

	private static void AddEventsModifiers()
	{
		var container = new SRDebugger.DynamicOptionContainer();
		// Create a mutable option

		int order = 12;
		foreach (var modifierObj in Enum.GetValues(typeof(EventModifier)))
		{
			var modifier = (EventModifier) modifierObj;

			CreateOption(modifier, order++, container);
		}

		var mts = new[]
		{
			MutatorType.Hardcore,
			MutatorType.Speed,
			MutatorType.HammerTime,
			MutatorType.RPGsOnly,
			MutatorType.SpecialsMayhem
		};
		foreach (var mutatorType in mts)
		{
			CreateMutator(mutatorType, order++, container);
		}

		SRDebug.Instance.AddOptionContainer(container);
	}

	private static void CreateMutator(MutatorType mutator, int order, DynamicOptionContainer container)
	{
		var definition = SRDebugger.OptionDefinition.Create(
			"mut:" + mutator,
			() => _mutators.Contains(mutator),
			(newValue) =>
			{
				if (newValue)
				{
					_mutators.Add(mutator);
				}
				else
				{
					_mutators.Remove(mutator);
				}
			},
			"Events",
			order
		);
		container.AddOption(definition);
	}

	private static void CreateOption(EventModifier modifier, int order, DynamicOptionContainer container)
	{
		var definition = SRDebugger.OptionDefinition.Create(
			modifier.ToString(),
			() => _toggledModifiers.Contains(modifier),
			(newValue) =>
			{
				if (newValue)
				{
					_toggledModifiers.Add(modifier);
				}
				else
				{
					_toggledModifiers.Remove(modifier);
				}
			},
			"Events",
			order
		);
		container.AddOption(definition);
	}

	private SimulationMatchConfig _originalConfig;

	[Category("Events")]
	[Sort(50)]
	public void Start()
	{
		var configProvider = MainInstaller.ResolveServices().ConfigsProvider;
		var config = configProvider.GetConfig<GameModeRotationConfig>();
		for (var slotIndex = 0; slotIndex < config.Slots.Count; slotIndex++)
		{
			var gmConfig = config.Slots[slotIndex];
			for (var entriyIndex = 0; entriyIndex < gmConfig.Entries.Count; entriyIndex++)
			{
				var gmConfigEntry = gmConfig.Entries[entriyIndex];
				if (gmConfigEntry.TimedEntry)
				{
					if (_originalConfig == null)
					{
						_originalConfig = gmConfigEntry.MatchConfig.CloneSerializing();
					}

					List<RewardModifier> rewardModifier = new List<RewardModifier>();
					List<MetaItemDropOverwrite> itemDropOverwrites = new List<MetaItemDropOverwrite>();

					foreach (var toggledModifier in _toggledModifiers)
					{
						switch (toggledModifier)
						{
							case EventModifier.DoubleBpp:
								rewardModifier.Add(new RewardModifier()
								{
									Id = GameId.BPP,
									Multiplier = FP._2
								});
								break;
							case EventModifier.DoubleTrophies:
								rewardModifier.Add(new RewardModifier()
								{
									Id = GameId.Trophies,
									Multiplier = FP._2
								});
								break;
							case EventModifier.DoubleXp:
								rewardModifier.Add(new RewardModifier()
								{
									Id = GameId.XP,
									Multiplier = FP._2
								});
								break;
							case EventModifier.DoubleNoobInGame:
								rewardModifier.Add(new RewardModifier()
								{
									Id = GameId.NOOB,
									Multiplier = FP._2,
									CollectedInsideGame = true,
								});
								break;
							case EventModifier.DrpNoobKill:
								itemDropOverwrites.Add(new MetaItemDropOverwrite()
								{
									Id = GameId.NOOB,
									Place = DropPlace.Player,
									DropRate = FP._1,
									MinDropAmount = 1,
									MaxDropAmount = 3,
								});
								break;

							case EventModifier.DrpCoinAir:
								itemDropOverwrites.Add(new MetaItemDropOverwrite()
								{
									Id = GameId.COIN,
									Place = DropPlace.Airdrop,
									DropRate = FP._1,
									MinDropAmount = 1,
									MaxDropAmount = 3,
								});
								break;
							case EventModifier.DrpBBChest:
								itemDropOverwrites.Add(new MetaItemDropOverwrite()
								{
									Id = GameId.BlastBuck,
									Place = DropPlace.Chest,
									DropRate = FP._1,
									MinDropAmount = 1,
									MaxDropAmount = 3,
								});
								break;
							case EventModifier.DrpCoinAir50:
								itemDropOverwrites.Add(new MetaItemDropOverwrite()
								{
									Id = GameId.COIN,
									Place = DropPlace.Airdrop,
									DropRate = FP._0_50,
									MinDropAmount = 1,
									MaxDropAmount = 3,
								});
								break;
							case EventModifier.DrpNoobKill50:
								itemDropOverwrites.Add(new MetaItemDropOverwrite()
								{
									Id = GameId.NOOB,
									Place = DropPlace.Player,
									DropRate = FP._0_50,
									MinDropAmount = 1,
									MaxDropAmount = 3,
								});
								break;
							case EventModifier.DrpBBChest50:
								itemDropOverwrites.Add(new MetaItemDropOverwrite()
								{
									Id = GameId.BlastBuck,
									Place = DropPlace.Chest,
									DropRate = FP._0_50,
									MinDropAmount = 1,
									MaxDropAmount = 3,
								});
								break;
						}
					}

					gmConfigEntry.TimedGameModeEntries = new List<DurationConfig>()
					{
						new ()
						{
							StartsAt = DateTime.UtcNow.AddMinutes(_startsIn).ToString(DurationConfig.DATE_FORMAT),
							EndsAt = DateTime.UtcNow.AddMinutes(_startsIn).AddMinutes(_eventDuration).ToString(DurationConfig.DATE_FORMAT)
						}
					};
					gmConfigEntry.MatchConfig.Mutators = _mutators.Select(selectedType =>
					{
						return configProvider.GetConfigsList<QuantumMutatorConfig>()
							.FirstOrDefault(mConfig => mConfig.Type == selectedType)
							?.Id;
					}).Where(cfg => cfg != null).ToArray();
					gmConfigEntry.MatchConfig.ConfigId = "debug-" + _originalConfig.ConfigId;
					gmConfigEntry.MatchConfig.RewardModifiers = rewardModifier.ToArray();
					gmConfigEntry.MatchConfig.MetaItemDropOverwrites = itemDropOverwrites.ToArray();
					gmConfig[slotIndex] = gmConfigEntry;
					config.Slots[slotIndex] = gmConfig;
					MainInstaller.ResolveServices().GameModeService.Init(config);
					FLog.Info(ModelSerializer.Serialize(MainInstaller.ResolveServices().GameModeService.Slots).Value);
					break;
				}
			}
		}
	}
}
#endif