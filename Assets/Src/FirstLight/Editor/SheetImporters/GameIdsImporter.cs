using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	[GoogleSheetImportOrder(0)]
	public class GameIdsImporter : IGoogleSheetConfigsImporter
	{
		private const string _name = "GameId";
		private const string _nameGroup = "GameIdGroup";
		private const string _idTag = "Id";
		private const string _groupsTag = "Groups";
		private const string _namespace = "FirstLight.Game.Ids";
		
		/// <inheritdoc />
		public string GoogleSheetUrl => "***REMOVED***/edit#gid=683619898";
		
		/// <inheritdoc />
		public void Import(List<Dictionary<string, string>> data)
		{
			var idList = new List<string>();
			var groupList = new List<string>();
			var mapGroups = new Dictionary<string, List<string>>();
			var mapIds = new Dictionary<string, List<string>>();
			
			foreach (var entry in data)
			{
				var groups = CsvParser.ArrayParse<string>(entry[_groupsTag]);
				var id = GetCleanName(entry[_idTag]);
				
				idList.Add(id);
				mapGroups.Add(id, groups);

				foreach (var group in groups)
				{
					var groupName = GetCleanName(group);
					if (!groupList.Contains(groupName))
					{
						groupList.Add(groupName);
						mapIds.Add(groupName, new List<string>());
					}
					
					mapIds[groupName].Add(id);
				}
			}
			GenerateQuantumQtn(idList, groupList);
			GenerateQuantumScript(mapGroups, mapIds);
			AssetDatabase.Refresh();
		}

		private static void GenerateQuantumQtn(IList<string> ids, IList<string> groups)
		{
			var stringBuilder = new StringBuilder();
			var fileIds = new Dictionary<string, int>();
			var fileGroups = new Dictionary<string, int>();
			var path = Application.dataPath.Replace("/Assets", $"/Quantum/quantum_code/quantum.code/Configs/{_name}.qtn");

			ProcessQuantumQtn(fileIds, fileGroups, path);

			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine($"enum {_name}");
			stringBuilder.AppendLine("{");
			GenerateEnum(stringBuilder, ids, fileIds);
			stringBuilder.AppendLine("}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"enum {_nameGroup}");
			stringBuilder.AppendLine("{");
			GenerateEnum(stringBuilder, groups, fileGroups);
			
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
			GenerateLoopUpMaps(stringBuilder, mapGroups, _name, _nameGroup, "groups");
			GenerateLoopUpMaps(stringBuilder, mapIds, _nameGroup, _name, "ids");
			stringBuilder.AppendLine("\t}");
			
			stringBuilder.AppendLine("}");

			File.WriteAllText(Application.dataPath.Replace("/Assets", path) , stringBuilder.ToString());
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
			stringBuilder.AppendLine($"\t\tprivate static readonly Dictionary<{element1Type}, ReadOnlyCollection<{element2Type}>> _{fieldName} =");
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

		private static void ProcessQuantumQtn(IDictionary<string, int> fileIds, IDictionary<string, int> fileGroups, string filePath)
		{
			var text = System.IO.File.ReadAllText(filePath);
			var idx1 = text.IndexOf('{') + 2;
			var idx2 = text.IndexOf('}') - 1;
			var idx3 = text.IndexOf('{', idx1) + 2;
			var idx4 = text.LastIndexOf('}') - 1;
			var ids = text.Substring(idx1, idx2 - idx1).Split('\n');
			var groups = text.Substring(idx3, idx4 - idx3).Split('\n');
			
			foreach (var id in ids)
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				var split = id.Split('=');
				var splitNumber = split[1].Trim();

				fileIds.Add(split[0].Trim(), int.Parse(splitNumber.Substring(0, splitNumber.Length - 1)));
			}

			foreach (var id in groups)
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				var split = id.Split('=');
				var splitNumber = split[1].Trim();

				fileGroups.Add(split[0].Trim(), int.Parse(splitNumber.Substring(0, splitNumber.Length - 1)));
			}
		}
		
		private static void GenerateEnum(StringBuilder stringBuilder, IList<string> ids, Dictionary<string, int> fileIds)
		{
			for (int i = 0, j = 0; i < ids.Count; i++)
			{
				if (!fileIds.TryGetValue(ids[i], out var value))
				{
					while (fileIds.ContainsValue(j))
					{
						j++;
					}
					value = j++;
				}
				
				stringBuilder.Append($"\t{ids[i]} = {value.ToString()},\n");
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
	}
}