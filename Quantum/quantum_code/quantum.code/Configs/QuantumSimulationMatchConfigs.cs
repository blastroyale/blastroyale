using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Inspector;

namespace Quantum
{
	[Serializable]
	public enum DropPlace : byte
	{
		Chest,
		Airdrop,
		Player
	}

	[Serializable]
	public enum MatchType : byte
	{
		Custom,
		Forced,
		Matchmaking,
	}

	[Serializable]
	public class MetaItemDropOverwrite
	{
		public GameId Id;
		public DropPlace Place;
		[Range(0, 1)] public FP DropRate;
		public int MinDropAmount;
		public int MaxDropAmount;

		public MetaItemDropOverwrite Clone()
		{
			return (MetaItemDropOverwrite) MemberwiseClone();
		}

		protected bool Equals(MetaItemDropOverwrite other)
		{
			return Id == other.Id && Place == other.Place && DropRate.Equals(other.DropRate) &&
				MinDropAmount == other.MinDropAmount && MaxDropAmount == other.MaxDropAmount;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((MetaItemDropOverwrite) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int) Id;
				hashCode = (hashCode * 397) ^ (int) Place;
				hashCode = (hashCode * 397) ^ DropRate.GetHashCode();
				hashCode = (hashCode * 397) ^ MinDropAmount;
				hashCode = (hashCode * 397) ^ MaxDropAmount;
				return hashCode;
			}
		}
	}

	[Serializable]
	public class RewardModifier
	{
		public GameId Id;
		public FP Multiplier;
		public bool CollectedInsideGame;

		public RewardModifier Clone()
		{
			return (RewardModifier) MemberwiseClone();
		}

		protected bool Equals(RewardModifier other)
		{
			return Id == other.Id && Multiplier.Equals(other.Multiplier) &&
				CollectedInsideGame == other.CollectedInsideGame;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((RewardModifier) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int) Id;
				hashCode = (hashCode * 397) ^ Multiplier.GetHashCode();
				hashCode = (hashCode * 397) ^ CollectedInsideGame.GetHashCode();
				return hashCode;
			}
		}
	}

	[Serializable]
	public class SimulationMatchConfig
	{
		public string ConfigId;
		public string GameModeID;
		public int MapId;
		public uint TeamSize;
		public Mutator Mutators;
		public string[] WeaponsSelectionOverwrite;
		public MetaItemDropOverwrite[] MetaItemDropOverwrites;
		public RewardModifier[] RewardModifiers;
		public bool HasBots;
		public int BotOverwriteDifficulty;
		public int MaxPlayersOverwrite;
		[HideInInspector] public MatchType MatchType;


		public byte[] ToByteArray()
		{
			var bitStream = new BitStream(new Byte[8192])
			{
				Writing = true
			};
			Serialize(bitStream);
			return bitStream.ToArray();
		}

		public static SimulationMatchConfig FromByteArray(byte[] bytes)
		{
			Assert.Always(bytes != null);
			var simulationConfig = new SimulationMatchConfig();
			var bitStream = new BitStream(bytes)
			{
				Reading = true
			};
			simulationConfig.Serialize(bitStream);
			return simulationConfig;
		}

		public void Serialize(BitStream stream)
		{
			stream.Serialize(ref ConfigId);
			var matchType = (byte) MatchType;
			stream.Serialize(ref matchType);
			MatchType = (MatchType) matchType;

			stream.Serialize(ref GameModeID);
			stream.Serialize(ref MapId);
			stream.Serialize(ref TeamSize);
			stream.Serialize(ref HasBots);
			stream.Serialize(ref BotOverwriteDifficulty);
			stream.Serialize(ref MaxPlayersOverwrite);
			WeaponsSelectionOverwrite ??= Array.Empty<string>();
			stream.SerializeArrayLength(ref WeaponsSelectionOverwrite);
			for (var i = 0; i < WeaponsSelectionOverwrite.Length; i++)
			{
				stream.Serialize(ref WeaponsSelectionOverwrite[i]);
			}

			var mutators = (int) Mutators;
			stream.Serialize(ref mutators);
			Mutators = (Mutator) mutators;

			MetaItemDropOverwrites ??= Array.Empty<MetaItemDropOverwrite>();
			stream.SerializeArrayLength(ref MetaItemDropOverwrites);
			for (var i = 0; i < MetaItemDropOverwrites.Length; i++)
			{
				MetaItemDropOverwrites[i] ??= new MetaItemDropOverwrite();
				var overwrite = MetaItemDropOverwrites[i];

				stream.Serialize(ref overwrite.Id);
				stream.Serialize(ref overwrite.DropRate);
				var intValue = (int) overwrite.Place;
				stream.Serialize(ref intValue);
				overwrite.Place = (DropPlace) intValue;
				stream.Serialize(ref overwrite.MinDropAmount);
				stream.Serialize(ref overwrite.MaxDropAmount);
			}

			RewardModifiers ??= Array.Empty<RewardModifier>();
			stream.SerializeArrayLength(ref RewardModifiers);
			for (var i = 0; i < RewardModifiers.Length; i++)
			{
				RewardModifiers[i] ??= new RewardModifier();
				var overwrite = RewardModifiers[i];
				stream.Serialize(ref overwrite.Id);
				stream.Serialize(ref overwrite.Multiplier);
				stream.Serialize(ref overwrite.CollectedInsideGame);
			}
		}
	}
}