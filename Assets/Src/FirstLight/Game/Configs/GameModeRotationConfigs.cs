using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using Newtonsoft.Json;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct GameModeRotationConfig
	{
		public long RotationStartTimeTicks;
		public uint RotationSlotDuration;

		public List<SlotWrapper> Slots;

		[Serializable]
		public class PlayfabQueue
		{
			[Required] public string QueueName;
			[Required] public int TeamSize;
			[Required] public int TimeoutTimeInSeconds;
		}

		[Serializable]
		public struct GameModeEntry : IEquatable<GameModeEntry>
		{
			public static string InvalidRewardsMessage = "You can only select the following values: " +
				string.Join(",", GameConstants.Data.AllowedGameRewards.Select(i => Enum.GetName(typeof(GameId), i)));

			public string GameModeId;
			public MatchType MatchType;
			public List<string> Mutators;
			public PlayfabQueue PlayfabQueue;
			[JsonIgnore]
			public int TeamSize => PlayfabQueue.TeamSize;

			[Required] [ValidateInput("ValidateAllowedRewards", "$InvalidRewardsMessage")]
			public List<GameId> AllowedRewards;

			[FoldoutGroup("Screen")] public string TitleTranslationKey;
			[FoldoutGroup("Screen")] public string DescriptionTranslationKey;
			[FoldoutGroup("Screen")] public string ImageModifier;


			private bool ValidateAllowedRewards(List<GameId> ids)
			{
				if (ids == null) return false;
				return ids.All(i => GameConstants.Data.AllowedGameRewards.Contains(i));
			}

			public override string ToString()
			{
				return
					$"{GameModeId}, {MatchType} TeamSize:{PlayfabQueue.TeamSize}, Mutators({string.Join(",", Mutators)})";
			}

			public bool Equals(GameModeEntry other)
			{
				return GameModeId == other.GameModeId &&
					MatchType == other.MatchType &&
					Mutators.SequenceEqual(other.Mutators) &&
					PlayfabQueue.Equals(other.PlayfabQueue) &&
					IsAllowedRewardsEqual(other.AllowedRewards);
			}

			private bool IsAllowedRewardsEqual(List<GameId> two)
			{
				if ((AllowedRewards == null || AllowedRewards.Count == 0) && (two == null || two.Count == 0))
				{
					return true;
				}

				if (AllowedRewards == null || two == null) return false;

				return AllowedRewards.SequenceEqual(two);
			}


			public override bool Equals(object obj)
			{
				return obj is GameModeEntry other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(GameModeId, (int) MatchType, Mutators, PlayfabQueue);
			}

			public static bool operator !=(GameModeEntry obj1, GameModeEntry obj2) => !(obj1.Equals(obj2));
			public static bool operator ==(GameModeEntry obj1, GameModeEntry obj2) => obj1.Equals(obj2);
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
	}
}