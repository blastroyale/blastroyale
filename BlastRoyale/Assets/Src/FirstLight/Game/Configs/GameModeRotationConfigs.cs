using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Quantum;
using UnityEngine;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Utils.Attributes;
using Sirenix.OdinInspector;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct GameModeRotationConfig
	{
		public List<SlotWrapper> Slots;

		[Serializable]
		public class PlayfabQueue
		{
			[Required] public string QueueName;
			[Required] public int TimeoutTimeInSeconds;
		}

		[Serializable]
		public class VisualEntryConfig
		{
			public LocalizableString TitleTranslationKey;
			public LocalizableString DescriptionTranslationKey;
			public string CardModifier;
			[SpriteClass] public string IconSpriteClass;
		}

		[Serializable]
		public struct GameModeEntry : IEquatable<GameModeEntry>
		{
			public bool TimedEntry;
			[ShowIf("TimedEntry")] public List<DurationConfig> TimedGameModeEntries;

			[FoldoutGroup("Match Config", expanded: true), HideLabel]
			public SimulationMatchConfig MatchConfig;

			[FoldoutGroup("Playfab Config", expanded: true), HideLabel]
			public PlayfabQueue PlayfabQueue;

			[FoldoutGroup("UI"), HideLabel] public VisualEntryConfig Visual;

			[JsonIgnore] public uint TeamSize => MatchConfig.TeamSize;

			public bool Equals(GameModeEntry other)
			{
				return TimedEntry == other.TimedEntry && Equals(TimedGameModeEntries, other.TimedGameModeEntries) && Equals(MatchConfig, other.MatchConfig) && Equals(PlayfabQueue, other.PlayfabQueue);
			}

			public override bool Equals(object obj)
			{
				return obj is GameModeEntry other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = TimedEntry.GetHashCode();
					hashCode = (hashCode * 397) ^ (TimedGameModeEntries != null ? TimedGameModeEntries.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (MatchConfig != null ? MatchConfig.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (PlayfabQueue != null ? PlayfabQueue.GetHashCode() : 0);
					return hashCode;
				}
			}

			public static bool operator !=(GameModeEntry obj1, GameModeEntry obj2) => !(obj1.Equals(obj2));
			public static bool operator ==(GameModeEntry obj1, GameModeEntry obj2) => obj1.Equals(obj2);

			public void ValidateConfigId(List<string> existingValues)
			{
				if (MatchConfig.TeamSize == 0)
				{
					MatchConfig.TeamSize = 1;
				}

				if (MatchConfig.MapId == 0)
				{
					MatchConfig.MapId = GameId.Any.GetHashCode();
				}

				while (string.IsNullOrEmpty(MatchConfig.ConfigId) || existingValues.Contains(MatchConfig.ConfigId))
				{
					var newValue = Guid.NewGuid().ToString("D");
					MatchConfig.ConfigId = newValue.Remove(newValue.IndexOf('-'));
				}
			}
		}

		[Serializable]
		public struct SlotWrapper
		{
			public List<GameModeEntry> Entries;

			public int Count => Entries.Count;

			public GameModeEntry this[int key]
			{
				get => Entries[key];
				set => Entries[key] = value;
			}
		}
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGameModeConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GameModeRotationConfigs",
		menuName = "ScriptableObjects/Configs/GameModeRotationConfigs")]
	public class GameModeRotationConfigs : ScriptableObject, ISingleConfigContainer<GameModeRotationConfig>
	{
		[SerializeField] private GameModeRotationConfig _config;

		public GameModeRotationConfig Config
		{
			get => _config;
			set => _config = value;
		}

		private void OnValidate()
		{
			var existing = new List<string>();
			foreach (var slotWrapper in Config.Slots)
			{
				foreach (var slotWrapperEntry in slotWrapper.Entries)
				{
					slotWrapperEntry.ValidateConfigId(existing);
					existing.Add(slotWrapperEntry.MatchConfig.ConfigId);
				}
			}
		}
	}
}