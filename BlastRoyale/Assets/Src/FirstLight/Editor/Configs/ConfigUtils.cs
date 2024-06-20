using System;
using System.Collections.Generic;
using FirstLightEditor;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.Configs
{
	/// <summary>
	/// Helper utils for Configs / importing.
	/// </summary>
	public static class ConfigUtils
	{
		/// <summary>
		/// Returns a dictionary of all importers and the types they import.
		/// </summary>
		public static Dictionary<Type, IGoogleSheetConfigsImporter> GetAllImporters()
		{
			var importerInterface = typeof(IGoogleSheetConfigsImporter);
			var soImporterInterface = typeof(IScriptableObjectImporter);
			var importers = new Dictionary<Type, IGoogleSheetConfigsImporter>();

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (!type.IsAbstract && !type.IsInterface && importerInterface.IsAssignableFrom(type) && soImporterInterface.IsAssignableFrom(type))
					{
						var importer = Activator.CreateInstance(type) as IGoogleSheetConfigsImporter;
						var soType = ((IScriptableObjectImporter) importer)!.ScriptableObjectType;
						
						importers.Add(soType, Activator.CreateInstance(type) as IGoogleSheetConfigsImporter);
					}
				}
			}

			return importers;
		}
	}
}