using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FirstLight.Game.Configs;
using Quantum;
using Quantum.Inspector;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

namespace FirstLight.Editor.Inspector
{

	public class MetaItemDropOverwriteProcessor : OdinAttributeProcessor<MetaItemDropOverwrite>
	{
		private static IEnumerable<GameId> ValidMetaItems = new[] {GameId.NOOB, GameId.COIN, GameId.BPP};

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			var className = GetType().GetNiceFullName();

			switch (member.Name)
			{
				case nameof(MetaItemDropOverwrite.Id):
				{
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(ValidMetaItems)}"));
					break;
				}
			}
		}
	}

	public class SimulationMatchProcessor : OdinAttributeProcessor<SimulationMatchConfig>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			var className = GetType().GetNiceFullName();
			if (attributes.Any(a => a.GetType() == typeof(HideInInspectorAttribute)))
			{
				attributes.Add(new HideIfAttribute("@true"));
				return;
			}

			switch (member.Name)
			{
				case nameof(SimulationMatchConfig.GameModeID):
				{
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GameModes)}"));
					break;
				}
				case nameof(SimulationMatchConfig.TeamSize):
				{
					attributes.Add(new PropertyRangeAttribute(1, 6));
					break;
				}
				case nameof(SimulationMatchConfig.MapId):
				{
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetMapIds)}()"));
					break;
				}
				case nameof(SimulationMatchConfig.Mutators):
				{
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(Mutators)}"));
					break;
				}
				case nameof(SimulationMatchConfig.BotOverwriteDifficulty):
				{
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetBotDifficulties)}(this.GameModeID)"));
					break;
				}
				case nameof(SimulationMatchConfig.MaxPlayersOverwrite):
				{
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetMaxPlayersOverwrite)}()"));
					break;
				}
				case nameof(SimulationMatchConfig.ConfigId):
				{
					attributes.Add(new ReadOnlyAttribute());
					break;
				}
			}
		}

		public static IEnumerable<string> GameModes => GameConfigsLoader.EditorConfigProvider.GetProvider().GetConfigsList<QuantumGameModeConfig>().Select(c => c.Id);
		public static IEnumerable<string> Mutators => Enum.GetValues(typeof(Mutator)).Cast<Mutator>().Select(m => m.ToString());

		public static ValueDropdownList<int> GetMaxPlayersOverwrite()
		{
			var values = new ValueDropdownList<int>();
			values.Add("Automatic", 0);
			values.AddRange(Enumerable.Range(1, 48).Select(a => new ValueDropdownItem<int>(a + " Players", a)));
			return values;
		}

		public static ValueDropdownList<int> GetMapIds()
		{
			var gms = new ValueDropdownList<int>();
			gms.Add("Any", GameId.Any.GetHashCode());
			foreach (var gm in GameConfigsLoader.EditorConfigProvider.GetProvider().GetConfigsList<QuantumMapConfig>())
			{
				gms.Add(gm.Map.ToString(), gm.Map.GetHashCode());
			}

			return gms;
		}

		public static ValueDropdownList<int> GetBotDifficulties(string gameMode)
		{
			var gms = new ValueDropdownList<int>();
			gms.Add("Automatic", -1);
			var botConfigs = GameConfigsLoader.EditorConfigProvider.LoadFromAddressable<BotConfigs>();
			foreach (var gm in botConfigs.Configs.Where(gm => gm.GameMode == gameMode))
			{
				gms.Add("Difficulty " + gm.Difficulty, (int) gm.Difficulty);
			}

			return gms;
		}
	}
}