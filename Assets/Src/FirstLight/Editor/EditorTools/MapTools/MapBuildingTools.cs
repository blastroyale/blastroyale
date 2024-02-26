using System;
using System.IO;
using FirstLight.Editor.EditorTools.Skins;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using LayerMask = UnityEngine.LayerMask;
using Object = UnityEngine.Object;


namespace FirstLight.Editor.EditorTools.MapTools
{
	public static class MapBuildingTool 
	{
		[MenuItem("FLG/Map/Create GOs with Quantum Colliders From Selected GOs &0")]
		private static void CreateQuantumColliders()
		{
			for (var i = 0; i < Selection.transforms.Length; i++)
			{
				var selected = Selection.transforms[i];
				
				var newGo = new GameObject("Wall COLL", typeof(QuantumStaticBoxCollider3D));
				newGo.transform.position = new Vector3(selected.position.x, 1.5f, selected.position.z);
				newGo.layer = 8;
				
				var coll = newGo.GetComponent<QuantumStaticBoxCollider3D>();
				coll.Size = new FPVector3(selected.localScale.x.ToFP(), FP._3, selected.localScale.z.ToFP());
			}
		}
	}
}