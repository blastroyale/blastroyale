using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FirstLight.Game.Utils;
using NUnit;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	public static class FlagMenuGenerator
	{
		private const string NAME = "FlagsEditorMenu";


		private static readonly List<FlagInfo> _flags;

		private static readonly List<SymbolInfo> _symbols = new()
		{
			new()
			{
				Name = "BotDebug",
				Description = "Enable bot debug visuals",
				Symbol = "DEBUG_BOTS"
			}
		};


		static FlagMenuGenerator()
		{
			_flags = GetFlags();
		}

		private enum FlagType
		{
			Enum,
			Bool,
		}

		private class SymbolInfo
		{
			public string Name;
			public string Symbol;
			public string Description;
			public string AccessorName => "Is" + Name;
			public string MenuItem => "FLG/Local Flags/Symbols/" + Description;
		}

		private class FlagInfo
		{
			public string Name;
			public string Description;
			public string AccessorName => "Is" + Name;
			public string MenuItem => "FLG/Local Flags/" + Description;
			public FieldInfo FieldInfo;
			public FlagType Type;
		}


		private static List<FlagInfo> GetFlags()
		{
			var flags = new List<FlagInfo>();
			foreach (var fieldInfo in typeof(LocalFeatureFlagConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				if (fieldInfo.FieldType != typeof(bool) && !fieldInfo.FieldType.IsEnum) continue;
				var attribute = fieldInfo.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
				flags.Add(new FlagInfo()
				{
					Name = fieldInfo.Name,
					FieldInfo = fieldInfo,
					Type = fieldInfo.FieldType == typeof(bool) ? FlagType.Bool : FlagType.Enum,
					Description = attribute != null ? attribute.Description : fieldInfo.Name
				});
			}

			return flags;
		}

		private static string Join(this IEnumerable<string> enumerable)
		{
			return string.Join("\n", enumerable);
		}

		private static string GenerateDelayCall()
		{
			return @$"	EditorApplication.delayCall += () =>
			{{
				{_flags
					.Where(f => f.Type == FlagType.Bool)
					.Select(f => $"Menu.SetChecked(\"{f.MenuItem}\", {f.AccessorName});")
					.Join()
				}

				{_symbols
					.Select(f => $"Menu.SetChecked(\"{f.MenuItem}\", {f.AccessorName});")
					.Join()
				}

				{_flags
					.Where(f => f.Type == FlagType.Enum)
					.Select(f => $"UpdateSelection{f.Name}();")
					.Join()
				}
			}};
";
		}

		private static string GenerateBoolAccessors()
		{
			return _flags.Where(f => f.Type == FlagType.Bool)
				.Select(flag => $@"
		private static bool {flag.AccessorName}
		{{
			get => FeatureFlags.GetLocalConfiguration().{flag.Name};
			set
			{{
				FeatureFlags.GetLocalConfiguration().{flag.Name} = value;
				Debug.Log(""Setting {flag.Name} to ""+value);
				FeatureFlags.SaveLocalConfig();
			}}
		}}
")
				.Join();
		}

		private static string GenerateBoolMenuItems()
		{
			return _flags.Where(f => f.Type == FlagType.Bool)
				.Select(flag => $@"
		[MenuItem(""{flag.MenuItem}"", false, 5)]
		private static void Toggle{flag.Name}()
		{{
			{flag.AccessorName} = !{flag.AccessorName};
			EditorApplication.delayCall += () => {{ Menu.SetChecked(""{flag.MenuItem}"", {flag.AccessorName}); }};
		}}
")
				.Join();
		}

		private static string GenerateSymbolAccessors()
		{
			return _symbols.Select(flag => $@"
				private static bool Is{flag.Name}
		{{
			get => FlagMenuGenerator.IsSymbolDefined(""{flag.Symbol}"");
			set => FlagMenuGenerator.SetCompileDefine(""{flag.Symbol}"", value);
		}}
")
				.Join();
		}

		private static string GenerateSymbolsMenuItem()
		{
			return _symbols
				.Select(flag => $@"
		[MenuItem(""{flag.MenuItem}"",false,30)]
		private static void Toggle{flag.Name}()
		{{
			{flag.AccessorName} = !{flag.AccessorName};
			EditorApplication.delayCall += () => {{ Menu.SetChecked(""{flag.MenuItem}"", {flag.AccessorName}); }};
		}}
")
				.Join();
		}


		private static string GenerateEnumMenuItem(FlagInfo flag)
		{
			var names = Enum.GetNames(flag.FieldInfo.FieldType);
			var str = new StringBuilder();
			str.Append($@"			
			private static void UpdateSelection{flag.Name}()
		{{
			var currentValue = {nameof(FeatureFlags)}.GetLocalConfiguration().{flag.Name};

			foreach (var name in Enum.GetNames(typeof({flag.FieldInfo.FieldType.FullName})))
			{{
				var menuPath = $""{flag.MenuItem}/{{name}}"";
				Menu.SetChecked(menuPath, currentValue.ToString() == name);
			}}
		}}");
			;
			foreach (var name in names)
			{
				var fullPathValue = flag.FieldInfo.FieldType.FullName + "." + name;
				var menuPath = $"{flag.MenuItem}/{name}";
				str.Append($@"
		[MenuItem(""{menuPath}"",false, 18)]
		private static void Toggle{flag.Name}{name}()
		{{
			
			FeatureFlags.GetLocalConfiguration().{flag.Name} = {fullPathValue};
			FeatureFlags.SaveLocalConfig();
			Debug.Log(""Setting {flag.Name} to {fullPathValue}"");
			EditorApplication.delayCall += UpdateSelection{flag.Name}; ;
		}}

	[MenuItem(""{menuPath}"",true,18)]
		private static bool Validate{flag.Name}{name}()
		{{
			var currentValue = {nameof(FeatureFlags)}.GetLocalConfiguration().{flag.Name};
			return currentValue.ToString() != ""{name}"";
		}}
");
			}

			return str.ToString();
		}


		[MenuItem("FLG/Generators/Generate Flags Menu")]
		public static void GenerateMenuFlagsMenu()
		{
			var path = $"/Assets/Src/FirstLight/Editor/EditorTools/Generated/{NAME}.cs";

			var content = $@"
// <auto-generated>
// This code was auto-generated by a tool, every time
// the tool executes this code will be reset.
// </auto-generated>
using FirstLight.Game.Utils;
using UnityEditor;
using System;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;


/* AUTO GENERATED CODE */
namespace FirstLight.Editor.EditorTools.Generated
{{
	[InitializeOnLoad]
	[SuppressMessage(""ReSharper"", ""InconsistentNaming"")]
		public static class FlagsEditorMenu
	{{



		static FlagsEditorMenu()
		{{
			{GenerateDelayCall()}
		}}

		{GenerateBoolAccessors()}
		{GenerateSymbolAccessors()}

		{GenerateBoolMenuItems()}
		{GenerateSymbolsMenuItem()}

		{_flags.Where(f => f.Type == FlagType.Enum).Select(GenerateEnumMenuItem).Join()}

	}}
}}";

			File.WriteAllText(Application.dataPath.Replace("/Assets", path), content);
		}


		public static void SetCompileDefine(string define, bool enabled)
		{
			var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			// Use hash set to remove duplicates.
			List<string> defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

			bool alreadyExists = false;

			for (var i = 0; i < defines.Count; i++)
			{
				if (string.Equals(define, defines[i], StringComparison.InvariantCultureIgnoreCase))
				{
					alreadyExists = true;
					if (!enabled)
					{
						defines.RemoveAt(i);
					}
				}
			}

			if (!alreadyExists && enabled)
			{
				defines.Add(define);
			}

			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines.ToArray()));
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
		}

		public static bool IsSymbolDefined(string symbol)
		{
			var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();
			return defines.Any(t => string.Equals(symbol, t, StringComparison.InvariantCultureIgnoreCase));
		}
		
	}
}