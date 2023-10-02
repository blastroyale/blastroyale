using System;
using System.IO;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using LayerMask = UnityEngine.LayerMask;
using Object = UnityEngine.Object;


namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class MapLayerFixer 
	{
		[MenuItem("FLG/Art/Fix Map Layers")]
		private static void OpenWindow()
		{
			foreach (var o in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (o.layer != LayerMask.NameToLayer("Default")) continue;
				if (o.GetComponentInChildren<QuantumStaticBoxCollider3D>() != null)
				{
					o.SetLayer(LayerMask.NameToLayer(PhysicsLayers.OBSTACLES));
				}
			}
		}

	}
}