using System.Linq;
using FirstLight.Editor.EditorTools.ArtTools;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Validate QuantumStaticBoxCollider3D gameObject owner scale is not negative
/// </summary>p
public class TechPassValidator
{
	[MenuItem("FLG/Art/Validators/MapValidation")]
	private static void ValidateMap()
	{
		var l = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		
		foreach(var go in l)
		{
			if (go.GetComponent<QuantumStaticBoxCollider3D>() != null)
			{
				var s = go.transform.localScale;
				if (s.x < 0 || s.y < 0 || s.z < 0)
				{
					Debug.LogError($"QCV | {go.FullGameObjectPath()} has negative scale");
				}
			}
		}
	}
}
