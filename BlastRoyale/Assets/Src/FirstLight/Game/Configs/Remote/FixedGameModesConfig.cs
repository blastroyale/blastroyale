using System;
using System.Collections.Generic;
using FirstLight.Game.Configs.Remote.FirstLight.Game.Configs.Remote;
using Newtonsoft.Json;
using Quantum;
using UnityEngine;
using FirstLight.Game.Configs.Utils;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public class FixedGameModeEntry : IGameModeEntry
	{
		public SimulationMatchConfig MatchConfig { get; set; }
		public LocalizableString Title { get; set; }
		public LocalizableString Description { get; set; }
		public LocalizableString LongDescription { get; set; }

		public string CardModifier;
	}

	[Serializable]
	public class FixedGameModesConfig : List<FixedGameModeEntry>
	{
	}
}