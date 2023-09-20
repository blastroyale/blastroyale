using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct GameModeRotationConfig
	{
		public long RotationStartTimeTicks;
		public uint RotationSlotDuration;

		public List<SlotWrapper> Slots;

		[Serializable]
		public struct GameModeEntry : IEquatable<GameModeEntry>
		{
			public static string InvalidRewardsMessage = "You can only select the following values: " +
				string.Join(",", GameConstants.Data.AllowedGameRewards.Select(i => Enum.GetName(typeof(GameId), i)));

			public string GameModeId;
			public MatchType MatchType;
			public List<string> Mutators;
			public bool Squads;
			public bool NFT;

			[Required] [ValidateInput("ValidateAllowedRewards", "$InvalidRewardsMessage")]
			public List<GameId> AllowedRewards;

			public GameModeEntry(string gameModeId, MatchType matchType, List<string> mutators, bool isSquads,
								 bool needNft, List<GameId> allowedRewards)
			{
				GameModeId = gameModeId;
				MatchType = matchType;
				Mutators = mutators;
				Squads = isSquads;
				NFT = needNft;
				AllowedRewards = allowedRewards;
			}

			private bool ValidateAllowedRewards(List<GameId> ids)
			{
				if (ids == null) return false;
				return ids.All(i => GameConstants.Data.AllowedGameRewards.Contains(i));
			}

			public override string ToString()
			{
				return
					$"{GameModeId}, {MatchType}{(Squads ? ", Squads" : "")}, Mutators({string.Join(",", Mutators)}){(NFT ? ", NFT" : "")}";
			}

			public bool Equals(GameModeEntry other)
			{
				return GameModeId == other.GameModeId &&
					MatchType == other.MatchType &&
					Mutators.SequenceEqual(other.Mutators) &&
					Squads == other.Squads &&
					NFT == other.NFT
					&& IsAllowedRewardsEqual(other.AllowedRewards);
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
				return HashCode.Combine(GameModeId, (int) MatchType, Mutators, Squads, NFT);
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