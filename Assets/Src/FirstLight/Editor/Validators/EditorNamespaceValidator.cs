using System.Collections;
using FirstLight.Editor.Validators;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;

[assembly: RegisterValidationRule(typeof(EditorNamespaceValidator), Name = "Editor Namespace Validator", Description = "Checks if there are any UnityEditor references where there shouldn't be.")]

namespace FirstLight.Editor.Validators
{
	/// <summary>
	/// Checks script files for UnityEditor references.
	/// </summary>
	public class EditorNamespaceValidator : GlobalValidator
	{
		public override IEnumerable RunValidation(ValidationResult result)
		{
			var scripts = AssetDatabase.FindAssets("t:Script", new[] {"Assets/Src/FirstLight/Game/"});

			foreach (var guid in scripts)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (scriptAsset.text.Contains("using UnityEditor") && !scriptAsset.text.StartsWith("#if UNITY_EDITOR"))
				{
					Debug.Log(scriptAsset.text);
					result.AddError($"Script {scriptAsset.name} contains UnityEditor reference.")
						.WithButton("Open Script", () => { AssetDatabase.OpenAsset(scriptAsset); });
				}

				yield return null;
			}
		}
	}
}