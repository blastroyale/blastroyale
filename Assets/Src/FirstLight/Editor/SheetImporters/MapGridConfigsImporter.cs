using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class MapGridConfigsImporter : GoogleSheetScriptableObjectImportContainer<MapGridConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=358135968";
		
		/// <inheritdoc />
		protected override void OnImport(MapGridConfigs scriptableObject, List<Dictionary<string, string>> data)
		{
			var grid = new List<MapGridRowConfig>(data.Count);

			for (var i = 0; i < data.Count; i++)
			{
				var row = data[i];
				var entry = new MapGridRowConfig { Row = new List<MapGridConfig>(row.Count) };

				for (var j = 0; j < row.Count; j++)
				{
					var val = row[j.ToString()];
					var config = new MapGridConfig
					{
						AreaName = val,
						X = j,
						Y = i
					};
					
					entry.Row.Add(config);
				}
				
				grid.Add(entry);
			}
			
			scriptableObject.SetData(grid);
		}
	}
}