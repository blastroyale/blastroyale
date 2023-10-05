using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Quantum;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace FirstLight.Editor.Ids
{
	public class Generator
	{
		private const string _name = "GameId";
		private const string _nameGroup = "GameIdGroup";
		private const string _namespace = "FirstLight.Game.Ids";


		/*[MenuItem("FLG/GOOOOOO")]
		 Used for converting the current game ids to the new format
		public static void GenerateBootstrap()
		{
			var values = SortByEnumOrder(Enum.GetValues(typeof(GameId)).Cast<GameId>());

			var str = new StringBuilder();
			foreach (var id in values)
			{
				var groups = string.Join(',', id.GetGroups().Select(g => "GroupSource." + g));
				if (groups != "")
				{
					groups = ", " + groups;
				}

				str.AppendLine($"{{\"{id}\", {(int) id} {groups}}},");
			}

			Debug.Log(str);
		}*/
        


		[MenuItem("FLG/Generators/Generate GameIds.qtn and GameIds.cs",priority = 20)]
		public static void GenerateIds()
		{
			Ids.GameIds.CheckDuplicates();
			var groups = SortByEnumOrder(Enum.GetValues(typeof(Ids.GroupSource)).Cast<Ids.GroupSource>()).ToList();
			var ids = Ids.GameIds.InternalList;
			var idsByGroup = new Dictionary<string, List<string>>();
			var groupsById = new Dictionary<string, List<string>>();
			foreach (var groupSource in groups)
			{
				idsByGroup[groupSource.ToString()] = new List<string>();
			}

			foreach (var gameIdEntry in ids)
			{
				groupsById[gameIdEntry.Name] = gameIdEntry.Groups.Select(g => g.ToString()).ToList();
				foreach (var groupSource in gameIdEntry.Groups)
				{
					idsByGroup[groupSource.ToString()].Add(gameIdEntry.Name);
				}
			}


			GenerateQuantumQtn(ids, groups);
			GenerateQuantumScript(idsByGroup, groupsById);
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("Success!", "Ids generated successfully!", "Ok");
		}

		private static void GenerateQuantumQtn(IList<GameIdEntry> ids, IList<Ids.GroupSource> groups)
		{
			var stringBuilder = new StringBuilder();
			var path = Application.dataPath.Replace("/Assets", $"/Quantum/quantum_code/quantum.code/Configs/{_name}.qtn");


			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine($"enum {_name}");
			stringBuilder.AppendLine("{");
			GenerateEnumIds(stringBuilder, ids);
			stringBuilder.AppendLine("}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"enum {_nameGroup}");
			stringBuilder.AppendLine("{");
			GenerateEnumGroups(stringBuilder, groups);

			stringBuilder.AppendLine("}");

			File.WriteAllText(path, stringBuilder.ToString());
		}

		private static void GenerateQuantumScript(Dictionary<string, List<string>> mapGroups, Dictionary<string, List<string>> mapIds)
		{
			var stringBuilder = new StringBuilder();
			var path = $"/Quantum/quantum_code/quantum.code/Configs/{_name}.cs";

			stringBuilder.AppendLine("using System.Collections.Generic;");
			stringBuilder.AppendLine("using System.Collections.ObjectModel;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine($"namespace Quantum");
			stringBuilder.AppendLine("{");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\tpublic class {_name}Comparer : IEqualityComparer<{_name}>");
			stringBuilder.AppendLine("\t{");
			stringBuilder.AppendLine($"\t\tpublic bool Equals({_name} x, {_name} y)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn x == y;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic int GetHashCode({_name} obj)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn (int)obj;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\tpublic class {_nameGroup}Comparer : IEqualityComparer<{_nameGroup}>");
			stringBuilder.AppendLine("\t{");
			stringBuilder.AppendLine($"\t\tpublic bool Equals({_nameGroup} x, {_nameGroup} y)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn x == y;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic int GetHashCode({_nameGroup} obj)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn (int)obj;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\tpublic static class {_name}Lookup");
			stringBuilder.AppendLine("\t{");
			GenerateLoopUpMethods(stringBuilder);
			GenerateLoopUpMaps(stringBuilder, mapIds, _name, _nameGroup, "groups");
			GenerateLoopUpMaps(stringBuilder, mapGroups, _nameGroup, _name, "ids");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("}");

			File.WriteAllText(Application.dataPath.Replace("/Assets", path), stringBuilder.ToString());
		}

		private static void GenerateLoopUpMethods(StringBuilder stringBuilder)
		{
			stringBuilder.AppendLine($"\t\tpublic static bool IsInGroup(this {_name} id, {_nameGroup} group)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\tif (!_groups.TryGetValue(id, out var groups))");
			stringBuilder.AppendLine("\t\t\t{");
			stringBuilder.AppendLine("\t\t\t\treturn false;");
			stringBuilder.AppendLine("\t\t\t}");
			stringBuilder.AppendLine("\t\t\treturn groups.Contains(group);");
			stringBuilder.AppendLine("\t\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic static IList<{_name}> GetIds(this {_nameGroup} group)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn _ids[group];");
			stringBuilder.AppendLine("\t\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic static IList<{_nameGroup}> GetGroups(this {_name} id)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn _groups[id];");
			stringBuilder.AppendLine("\t\t}");
		}

		private static void GenerateLoopUpMaps(StringBuilder stringBuilder, Dictionary<string, List<string>> map,
											   string element1Type, string element2Type, string fieldName)
		{
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine(
				$"\t\tprivate static readonly Dictionary<{element1Type}, ReadOnlyCollection<{element2Type}>> _{fieldName} =");
			stringBuilder.AppendLine($"\t\t\tnew Dictionary<{element1Type}, ReadOnlyCollection<{element2Type}>> (new {element1Type}Comparer())");
			stringBuilder.AppendLine("\t\t\t{");

			foreach (var pair in map)
			{
				stringBuilder.AppendLine("\t\t\t\t{");
				stringBuilder.AppendLine($"\t\t\t\t\t{element1Type}.{pair.Key}, new List<{element2Type}>");
				stringBuilder.AppendLine("\t\t\t\t\t{");
				for (var i = 0; i < pair.Value.Count; i++)
				{
					stringBuilder.Append("\t\t\t\t\t\t");
					stringBuilder.Append($"{element2Type}.{pair.Value[i]}");
					stringBuilder.Append(i + 1 == pair.Value.Count ? "\n" : ",\n");
				}

				stringBuilder.AppendLine("\t\t\t\t\t}.AsReadOnly()");
				stringBuilder.AppendLine("\t\t\t\t},");
			}

			stringBuilder.AppendLine("\t\t\t};");
		}


		private static void GenerateEnumIds(StringBuilder stringBuilder, IList<GameIdEntry> ids)
		{
			foreach (var t in ids)
			{
				stringBuilder.Append($"\t{t.Name} = {t.Id.ToString()},\n");
			}
		}

		private static void GenerateEnumGroups(StringBuilder stringBuilder, IList<Ids.GroupSource> ids)
		{
			foreach (var t in ids)
			{
				stringBuilder.Append($"\t{t.ToString()} = {(int) t},\n");
			}
		}

		private static string GetCleanName(string name)
		{
			return name.Replace(' ', '_');
		}

		private static void GenerateScript(IList<string> ids, IList<string> groups, Dictionary<string, List<string>> mapGroups,
										   Dictionary<string, List<string>> mapIds)
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("using I2.Loc;");
			stringBuilder.AppendLine("using Quantum;");
			stringBuilder.AppendLine("using System.Collections.Generic;");
			stringBuilder.AppendLine("using System.Collections.ObjectModel;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine($"namespace {_namespace}");
			stringBuilder.AppendLine("{");

			stringBuilder.AppendLine("\t[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]");
			stringBuilder.AppendLine($"\tpublic enum {_name}");
			stringBuilder.AppendLine("\t{");

			for (var i = 0; i < ids.Count; i++)
			{
				stringBuilder.Append($"\t{ids[i]} = {i.ToString()},\n");
			}

			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("\t[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]");
			stringBuilder.AppendLine($"\tpublic enum {_nameGroup}");
			stringBuilder.AppendLine("\t{");

			for (var i = 0; i < groups.Count; i++)
			{
				stringBuilder.Append($"\t{groups[i]} = {i.ToString()},\n");
			}

			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\tpublic class {_name}Comparer : IEqualityComparer<{_name}>");
			stringBuilder.AppendLine("\t{");
			stringBuilder.AppendLine($"\t\tpublic bool Equals({_name} x, {_name} y)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn x == y;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic int GetHashCode({_name} obj)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn (int)obj;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\tpublic class {_nameGroup}Comparer : IEqualityComparer<{_nameGroup}>");
			stringBuilder.AppendLine("\t{");
			stringBuilder.AppendLine($"\t\tpublic bool Equals({_nameGroup} x, {_nameGroup} y)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn x == y;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic int GetHashCode({_nameGroup} obj)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn (int)obj;");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\tpublic static class {_name}Lookup");
			stringBuilder.AppendLine("\t{");
			GenerateLoopUpMethods(stringBuilder);
			GenerateLoopUpMaps(stringBuilder, mapGroups, _name, _nameGroup, "groups");
			GenerateLoopUpMaps(stringBuilder, mapIds, _nameGroup, _name, "ids");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("}");

			SaveScript(stringBuilder.ToString());
		}

		private static void SaveScript(string scriptString)
		{
			var scriptAssets = AssetDatabase.FindAssets($"t:Script {_name}");
			var scriptPath = $"Assets/{_name}.cs";

			foreach (var scriptAsset in scriptAssets)
			{
				var path = AssetDatabase.GUIDToAssetPath(scriptAsset);
				if (path.EndsWith($"/{_name}.cs"))
				{
					scriptPath = path;
					break;
				}
			}

			File.WriteAllText(scriptPath, scriptString);
		}
		
		private static IEnumerable<T> SortByEnumOrder<T>(IEnumerable<T> original) where T : Enum
		{
			return original.OrderBy(id =>
			{
				int index = 0;
				foreach (var fieldInfo in typeof(T).GetFields()
							 .Where(fi => fi.IsStatic).OrderBy(fi => fi.MetadataToken))
				{
					if (fieldInfo.Name == id.ToString())
					{
						return index;
					}

					index++;
				}

				return index;
			});
		}
	}
}