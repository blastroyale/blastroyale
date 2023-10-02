using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Quantum;

namespace FirstLight.Game.Serializers
{
	/// <summary>
	///     Temporary struct used to deserialize legacy data stored using the object format,
	///     So if we change the Equipment struct we can still deserialize old datas.
	///     Also this is a workaround to avoid infinite recursion
	/// </summary>
	[Serializable]
	internal struct EquipmentObjectJsonTemp
	{
		public EquipmentAdjective Adjective;
		public EquipmentEdition Edition;
		public EquipmentFaction Faction;
		public GameId GameId;
		public uint Generation;
		public EquipmentGrade Grade;
		public uint InitialReplicationCounter;
		public long LastRepairTimestamp;
		public uint Level;
		public EquipmentMaterial Material;
		public uint MaxDurability;
		public EquipmentRarity Rarity;
		public uint ReplicationCounter;
		public ulong TotalRestoredDurability;
		public uint Tuning;

		public Equipment ToEquipment()
		{
			return new Equipment
			{
				Adjective = Adjective,
				Edition = Edition,
				Faction = Faction,
				GameId = GameId,
				Generation = Generation,
				Grade = Grade,
				InitialReplicationCounter = InitialReplicationCounter,
				LastRepairTimestamp = LastRepairTimestamp,
				Level = Level,
				Material = Material,
				MaxDurability = MaxDurability,
				Rarity = Rarity,
				ReplicationCounter = ReplicationCounter,
				TotalRestoredDurability = TotalRestoredDurability,
				Tuning = Tuning
			};
		}
	}

	internal class EquipmentEnumConvertersV0
	{
		public readonly IReadOnlyDictionary<string, GameId> GameIdMapping;

		public EquipmentEnumConvertersV0()
		{
			GameIdMapping = Enum.GetValues(typeof(GameId)).Cast<GameId>().ToDictionary(e => e.ToString(), e => e);
		}

		public EnumStaticMap<EquipmentAdjective> AdjectiveMapping { get; } = new(
			new Dictionary<EquipmentAdjective, int>
			{
				{EquipmentAdjective.Regular, 0},
				{EquipmentAdjective.Cool, 1},
				{EquipmentAdjective.Ornate, 2},
				{EquipmentAdjective.Posh, 3},
				{EquipmentAdjective.Exquisite, 4},
				{EquipmentAdjective.Majestic, 5},
				{EquipmentAdjective.Marvelous, 6},
				{EquipmentAdjective.Magnificent, 7},
				{EquipmentAdjective.Royal, 8},
				{EquipmentAdjective.Divine, 9}
			}
		);

		public EnumStaticMap<EquipmentEdition> EditionMapping { get; } = new(
			new Dictionary<EquipmentEdition, int>
			{
				{EquipmentEdition.Genesis, 0}
			}
		);

		public EnumStaticMap<EquipmentFaction> FactionMapping { get; } = new(
			new Dictionary<EquipmentFaction, int>
			{
				{EquipmentFaction.Order, 0},
				{EquipmentFaction.Chaos, 1},
				{EquipmentFaction.Organic, 2},
				{EquipmentFaction.Dark, 3},
				{EquipmentFaction.Shadow, 4},
				{EquipmentFaction.Celestial, 5},
				{EquipmentFaction.Dimensional, 6}
			}
		);

		public EnumStaticMap<EquipmentGrade> GradeMapping { get; } = new(
			new Dictionary<EquipmentGrade, int>
			{
				{EquipmentGrade.GradeI, 0},
				{EquipmentGrade.GradeII, 1},
				{EquipmentGrade.GradeIII, 2},
				{EquipmentGrade.GradeIV, 3},
				{EquipmentGrade.GradeV, 4}
			}
		);

		public EnumStaticMap<EquipmentMaterial> MaterialMapping { get; } = new(
			new Dictionary<EquipmentMaterial, int>
			{
				{EquipmentMaterial.Plastic, 0},
				{EquipmentMaterial.Steel, 1},
				{EquipmentMaterial.Bronze, 2},
				{EquipmentMaterial.Carbon, 3},
				{EquipmentMaterial.Golden, 4}
			}
		);

		public EnumStaticMap<EquipmentRarity> RarityMapping { get; } = new(
			new Dictionary<EquipmentRarity, int>
			{
				{EquipmentRarity.Common, 0},
				{EquipmentRarity.CommonPlus, 1},
				{EquipmentRarity.Uncommon, 2},
				{EquipmentRarity.UncommonPlus, 3},
				{EquipmentRarity.Rare, 4},
				{EquipmentRarity.RarePlus, 5},
				{EquipmentRarity.Epic, 6},
				{EquipmentRarity.EpicPlus, 7},
				{EquipmentRarity.Legendary, 8},
				{EquipmentRarity.LegendaryPlus, 9}
			}
		);
	}

	public class EquipmentSerializer : JsonConverter<Equipment>
	{
		private const int CURRENT_VERSION = 0;

		private static readonly EquipmentEnumConvertersV0 Converter = new();
		
		/// <summary>
		/// Just in case we need to debug, good to keep track.
		/// This is used in server
		/// </summary>
		public static readonly string[] FIELDS_FOR_REFERENCE =
		{
			"version", "gameid", "adjective", "edition", "faction", "grade", "material", "rarity", "generation", "initialReplicationCounter",
			"lastRepair", "level", "maxDurability", "replicationCounter", "totalRestoredDurability", "tuning"
		};
		
		public override void WriteJson(JsonWriter writer, Equipment value, JsonSerializer serializer)
		{
			writer.WriteStartArray();

			writer.WriteValue(CURRENT_VERSION);
			// Game id must be string i don't trust people will not keep changing the order and values
			writer.WriteValue(value.GameId.ToString());
			writer.WriteValue(Converter.AdjectiveMapping.GetIntValue(value.Adjective));
			writer.WriteValue(Converter.EditionMapping.GetIntValue(value.Edition));
			writer.WriteValue(Converter.FactionMapping.GetIntValue(value.Faction));
			writer.WriteValue(Converter.GradeMapping.GetIntValue(value.Grade));
			writer.WriteValue(Converter.MaterialMapping.GetIntValue(value.Material));
			writer.WriteValue(Converter.RarityMapping.GetIntValue(value.Rarity));
			writer.WriteValue(value.Generation);
			writer.WriteValue(value.InitialReplicationCounter);
			writer.WriteValue(value.LastRepairTimestamp.ToString());
			writer.WriteValue(value.Level);
			writer.WriteValue(value.MaxDurability);
			writer.WriteValue(value.ReplicationCounter);
			writer.WriteValue(value.TotalRestoredDurability);
			writer.WriteValue(value.Tuning);

			writer.WriteEndArray();
		}

		public override Equipment ReadJson(JsonReader reader, Type objectType, Equipment existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			// here the library already read the first token
			var first = reader.TokenType;
			if (first == JsonToken.StartObject)
				// Use old deserializer
				return serializer.Deserialize<EquipmentObjectJsonTemp>(reader).ToEquipment();

			if (reader.TokenType == JsonToken.StartArray) return ReadAsArray(reader, objectType, existingValue, hasExistingValue, serializer);

			throw new JsonException($"Unexpected token type {reader.TokenType}");
		}


		private uint ReadUint(JsonReader reader)
		{
			// We don't have uints here that may overflow so idc
			var value = reader.ReadAsInt32();
			if (value is null or < 0) throw new InvalidOperationException("Cant parse uint!");

			return (uint) value;
		}

		private long ReadStringAsLong(JsonReader reader)
		{
			var value = reader.ReadAsString();
			if (!string.IsNullOrEmpty(value) && long.TryParse(value, out var parsed)) return parsed;

			throw new InvalidOperationException("Cant parse long!");
		}


		private Equipment ReadAsArray(JsonReader reader, Type objectType, Equipment existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			Equipment equipment = new();

			var version = reader.ReadAsInt32();
			if (version != CURRENT_VERSION) throw new InvalidOperationException($"Invalid version number. Expected {CURRENT_VERSION}, but got {version}.");

			var gameIdString = reader.ReadAsString();
			if (gameIdString == null || !Converter.GameIdMapping.TryGetValue(gameIdString, out var id)) throw new InvalidOperationException($"{gameIdString} is not a valid game id");

			equipment.GameId = id;
			equipment.Adjective = Converter.AdjectiveMapping.GetValue(reader.ReadAsInt32());
			equipment.Edition = Converter.EditionMapping.GetValue(reader.ReadAsInt32());
			equipment.Faction = Converter.FactionMapping.GetValue(reader.ReadAsInt32());
			equipment.Grade = Converter.GradeMapping.GetValue(reader.ReadAsInt32());
			equipment.Material = Converter.MaterialMapping.GetValue(reader.ReadAsInt32());
			equipment.Rarity = Converter.RarityMapping.GetValue(reader.ReadAsInt32());
			equipment.Generation = ReadUint(reader);
			equipment.InitialReplicationCounter = ReadUint(reader);
			equipment.LastRepairTimestamp = ReadStringAsLong(reader);
			equipment.Level = ReadUint(reader);
			equipment.MaxDurability = ReadUint(reader);
			equipment.ReplicationCounter = ReadUint(reader);
			equipment.TotalRestoredDurability = ReadUint(reader);
			equipment.Tuning = ReadUint(reader);

			reader.Read(); // End array

			return equipment;
		}
	}
}