using System.Collections.Generic;
using System.IO;
using System.Text;
using FirstLight.AddressablesExtensions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.AddressablesExtensions
{
	/// <summary>
	/// Generates the Addressable Ids into a script file
	/// </summary>
	public static class AddressableIdsGenerator
	{
		private const string _objectName = "AddressableId";
		private const string _namespace = "FirstLight.Game.Ids";
		private const string _generateLabel = "GenerateIds";
		
		[MenuItem("Tools/Generate AddressableIds")]
		private static void GenerateAddressableIds()
		{
			var assetList = GetAssetList();
			
			ProcessData(assetList, out var labelMap, out var paths);
			GenerateScript(assetList, labelMap, paths);

			AssetDatabase.Refresh();
		}
		
		private static List<AddressableAssetEntry> GetAssetList()
		{
			var assetList = new List<AddressableAssetEntry>();
			var assetsSettings = AddressableAssetSettingsDefaultObject.Settings;
			
			foreach (var settingsGroup in assetsSettings.groups)
			{
				if (settingsGroup.ReadOnly)
				{
					continue;
				}
				
				settingsGroup.GatherAllAssets(assetList, true, true, false);
			}

			return assetList;
		}

		private static void SaveScript(string scriptString)
		{
			var scriptAssets = AssetDatabase.FindAssets($"t:Script {_objectName}");
			var scriptPath = $"Assets/{_objectName}.cs";

			foreach (var scriptAsset in scriptAssets)
			{
				var path = AssetDatabase.GUIDToAssetPath(scriptAsset);
				if (path.EndsWith($"/{_objectName}.cs"))
				{
					scriptPath = path;
					break;
				}
			}

			File.WriteAllText(scriptPath, scriptString);
		}

		private static void GenerateScript(List<AddressableAssetEntry> assetList, 
		                                     Dictionary<string, IList<AddressableAssetEntry>> labelMap,
		                                     List<string> paths)
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("using System.Collections.Generic;");
			stringBuilder.AppendLine("using System.Collections.ObjectModel;");
			stringBuilder.AppendLine("using FirstLight.AddressablesExtensions;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("// ReSharper disable InconsistentNaming");
			stringBuilder.AppendLine("// ReSharper disable once CheckNamespace");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"namespace {_namespace}");
			stringBuilder.AppendLine("{");
			
			stringBuilder.AppendLine($"\tpublic enum {_objectName}");
			stringBuilder.AppendLine("\t{");
			GenerateAddressEnums(stringBuilder, assetList);
			stringBuilder.AppendLine("\t}");
			
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("\tpublic enum AddressableLabel");
			stringBuilder.AppendLine("\t{");
			GenerateLabelEnums(stringBuilder, new List<string>(labelMap.Keys));
			stringBuilder.AppendLine("\t}");
			
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("\tpublic static class AddressablePathLookup");
			stringBuilder.AppendLine("\t{");
			GeneratePaths(stringBuilder, paths);
			stringBuilder.AppendLine("\t}");
			
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("\tpublic static class AddressableConfigLookup");
			stringBuilder.AppendLine("\t{");
			GenerateLoopUpMethods(stringBuilder);
			GenerateLabelMap(stringBuilder, labelMap);
			GenerateConfigs(stringBuilder, assetList);
			stringBuilder.AppendLine("\t}");
			
			stringBuilder.AppendLine("}");

			SaveScript(stringBuilder.ToString());
		}

		private static void GenerateLoopUpMethods(StringBuilder stringBuilder)
		{
			stringBuilder.AppendLine($"\t\tpublic static IList<{nameof(AddressableConfig)}> Configs => _addressableConfigs;");
			stringBuilder.AppendLine($"\t\tpublic static IList<string> Labels => _addressableLabels;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic static {nameof(AddressableConfig)} GetConfig(this {_objectName} addressable)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn _addressableConfigs[(int) addressable];");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic static IList<{nameof(AddressableConfig)}> GetConfigs(this AddressableLabel label)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn _addressableLabelMap[_addressableLabels[(int) label]];");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic static IList<{nameof(AddressableConfig)}> GetConfigs(string label)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn _addressableLabelMap[label];");
			stringBuilder.AppendLine("\t\t}");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tpublic static string ToLabelString(this AddressableLabel label)");
			stringBuilder.AppendLine("\t\t{");
			stringBuilder.AppendLine("\t\t\treturn _addressableLabels[(int) label];");
			stringBuilder.AppendLine("\t\t}");
		}

		private static void GenerateLabelMap(StringBuilder stringBuilder, IDictionary<string, IList<AddressableAssetEntry>> assetLabelMap)
		{
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("\t\tprivate static readonly IList<string> _addressableLabels = new List<string>");
			stringBuilder.AppendLine("\t\t{");

			if (assetLabelMap.Count > 0)
			{
				stringBuilder.AppendLine($"\t\t\t{GenerateLabels(new List<string>(assetLabelMap.Keys))}");
			}
			
			stringBuilder.AppendLine("\t\t}.AsReadOnly();");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tprivate static readonly IReadOnlyDictionary<string, IList<{nameof(AddressableConfig)}>> _addressableLabelMap = new ReadOnlyDictionary<string, IList<{nameof(AddressableConfig)}>>(new Dictionary<string, IList<{nameof(AddressableConfig)}>>");
			stringBuilder.AppendLine("\t\t{");

			foreach (var labelMap in assetLabelMap)
			{
				stringBuilder.AppendLine($"\t\t\t{{\"{labelMap.Key}\", new List<{nameof(AddressableConfig)}>");
				stringBuilder.AppendLine("\t\t\t\t{");
				
				for (var i = 0; i < labelMap.Value.Count; i++)
				{
					stringBuilder.AppendLine($"\t\t\t\t\t{GenerateAddressableConfig(labelMap.Value[i], i)},");
				}

				stringBuilder.AppendLine("\t\t\t\t}.AsReadOnly()}");
			}

			stringBuilder.AppendLine("\t\t});");
		}

		private static void GeneratePaths(StringBuilder stringBuilder, IList<string> paths)
		{
			for (var i = 0; i < paths.Count; i++)
			{
				stringBuilder.AppendLine($"\t\tpublic static readonly string {GetCleanName(paths[i], false)} = \"{paths[i]}\";");
			}
		}

		private static void GenerateConfigs(StringBuilder stringBuilder, IReadOnlyList<AddressableAssetEntry> assetList)
		{
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine($"\t\tprivate static readonly IList<{nameof(AddressableConfig)}> _addressableConfigs = new List<{nameof(AddressableConfig)}>");
			stringBuilder.AppendLine("\t\t{");

			for (var i = 0; i < assetList.Count; i++)
			{
				stringBuilder.Append($"\t\t\t{GenerateAddressableConfig(assetList[i], i)}");
				stringBuilder.Append(i + 1 == assetList.Count ? "\n" : ",\n");
			}

			stringBuilder.AppendLine("\t\t}.AsReadOnly();");
		}

		private static string GenerateLabels(IList<string> labels)
		{
			var stringBuilder = new StringBuilder();
			
			if (labels.Count == 0)
			{
				stringBuilder.Append("\"\"");
			}
			
			for (var i = 0; i < labels.Count; i++)
			{
				stringBuilder.Append($"\"{labels[i]}\"");
				stringBuilder.Append(i + 1 == labels.Count ? "" : ",");
			}

			return stringBuilder.ToString();
		}

		private static string GenerateAddressableConfig(AddressableAssetEntry addressableAssetEntry, int index)
		{
			var asseType = AssetDatabase.GetMainAssetTypeAtPath(addressableAssetEntry.AssetPath);

			asseType = asseType == typeof(UnityEditor.SceneAsset) ? typeof(UnityEngine.SceneManagement.Scene) : asseType;
			
			return $"new {nameof(AddressableConfig)}({index.ToString()}, \"{addressableAssetEntry.address}\", \"{addressableAssetEntry.AssetPath}\", " +
			       $"typeof({asseType}), new [] {{{GenerateLabels(new List<string>(addressableAssetEntry.labels))}}})";
		}

		private static void ProcessData(IList<AddressableAssetEntry> assetList,
		                                out Dictionary<string, IList<AddressableAssetEntry>> labelMap, 
		                                out List<string> paths)
		{
			labelMap = new Dictionary<string, IList<AddressableAssetEntry>>();
			paths = new List<string>();
			
			for (var i = assetList.Count - 1; i > -1; --i)
			{
				foreach (var label in assetList[i].labels)
				{
					if (label != _generateLabel)
					{
						continue;
					}
					
					if (!labelMap.TryGetValue(label, out var list))
					{
						list = new List<AddressableAssetEntry>();
						labelMap.Add(label, list);
					}
					
					list.Add(assetList[i]);
				}
				
				if (!assetList[i].labels.Contains(_generateLabel))
				{
					assetList.RemoveAt(i);
					continue;
				}
				
				var address = assetList[i].address;
				var pathLastCharIndex = address.Replace('\\', '/').LastIndexOf('/');
				var path = pathLastCharIndex < 0 ? address : address.Substring(0,  pathLastCharIndex);

				if (!paths.Contains(path))
				{
					paths.Add(path);
				}
			}
		}

		private static void GenerateAddressEnums(StringBuilder stringBuilder, IReadOnlyList<AddressableAssetEntry> assetList)
		{
			var addedNames = new List<string>();
			
			for (var i = 0; i < assetList.Count; i++)
			{
				var name = GetCleanName(assetList[i].address, true);
				var filetype = assetList[i].address.Substring(assetList[i].address.LastIndexOf('.') + 1);

				name = addedNames.Contains(name) ? $"{name}_{filetype}" : name;
				
				addedNames.Add(name);

				stringBuilder.Append("\t\t");
				stringBuilder.Append(GetCleanName(assetList[i].address, true));
				stringBuilder.Append(i + 1 == assetList.Count ? "\n" : ",\n");
			}
		}

		private static void GenerateLabelEnums(StringBuilder stringBuilder, IList<string> labels)
		{
			for (var i = 0; i < labels.Count; i++)
			{
				stringBuilder.Append("\t\tLabel_");
				stringBuilder.Append(GetCleanName(labels[i], true));
				stringBuilder.Append(i + 1 == labels.Count ? "\n" : ",\n");
			}
		}
		
		private static string GetCleanName(string name, bool withUnderscore)
		{
			var index = name.LastIndexOf('.');
			var charReplace = withUnderscore ? "_" : "";
			
			name = index < 0 ? name : name.Substring(0, index);
			name = name.Replace("/", charReplace);
			name = name.Replace("\\", charReplace);
			name = name.Replace(" ", charReplace);
			name = name.Replace("-", charReplace);

			return name;
		}
	}
}
