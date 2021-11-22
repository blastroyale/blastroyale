using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.GoogleSheetImporter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class AdventureConfigsImporter : GoogleSheetQuantumConfigsImporter<AdventureConfig, AdventureConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=1605130118";
	}
}