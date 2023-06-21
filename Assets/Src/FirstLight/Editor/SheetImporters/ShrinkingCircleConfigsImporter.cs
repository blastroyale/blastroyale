using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLightServerSDK.Modules;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class ShrinkingCircleConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumShrinkingCircleConfig, ShrinkingCircleConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1238613754";
		
		protected override QuantumShrinkingCircleConfig Deserialize(Dictionary<string, string> data)
		{
			var config = base.Deserialize(data);
			
			config.Key = config.GetHashCode();
			
			return config;
		}
	}
}