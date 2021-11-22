using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLightEditor.GoogleSheetImporter;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	/// <remarks>
	/// This google sheet importer extends the behaviour to help with quantum data loading
	/// </remarks>
	public abstract class GoogleSheetQuantumConfigsImporter<TConfig, TScriptableObject> : GoogleSheetConfigsImporter<TConfig, TScriptableObject>
		where TConfig : struct
		where TScriptableObject : ScriptableObject, IConfigsContainer<TConfig>
	{
		protected override TConfig Deserialize(Dictionary<string, string> data)
		{
			return QuantumDeserializer.DeserializeTo<TConfig>(data);
		}
	}
}