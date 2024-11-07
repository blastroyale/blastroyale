using System;
using FirstLight.Game.Configs.Utils;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using Quantum;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Configs.Remote
{
	public interface IGameModeEntry
	{
		SimulationMatchConfig MatchConfig { get; }

		LocalizableString Title { get; }
		LocalizableString Description { get; }

		LocalizableString LongDescription { get; }
	}

	[Serializable]
	public class EventGameModeEntry : IEquatable<EventGameModeEntry>, IGameModeEntry
	{
		public SimulationMatchConfig MatchConfig { get; set; }
		public LocalizableString Title { get; set; }
		public LocalizableString Description { get; set; }
		public LocalizableString LongDescription { get; set; }
		public List<DurationConfig> Schedule;
		public string ImageURL;
		public LegacyItemData PriceToJoin;

		public bool IsPaid => PriceToJoin != null;

		public bool Equals(EventGameModeEntry other)
		{
			return MatchConfig?.UniqueConfigId == other.MatchConfig?.UniqueConfigId;
		}
	}

	[Serializable]
	public class EventGameModesConfig : List<EventGameModeEntry>
	{
	}
}