using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class MapConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumMapConfig, MapConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1605130118";
	}
}