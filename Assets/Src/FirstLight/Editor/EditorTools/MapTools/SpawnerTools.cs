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


namespace FirstLight.Editor.EditorTools.ArtTools
{
	public static class SpawnerTool 
	{
		[MenuItem("FLG/Map/Spawners/Show")]
		private static void ShowSpawners()
		{
			foreach (var o in Object.FindObjectsByType<SpawnerDebugMonoComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				_ = o.ShowDebugIcon();
			}
		}
		
		[MenuItem("FLG/Map/Spawners/Hide")]
		private static void HideSpawners()
		{
			foreach (var o in Object.FindObjectsByType<SpawnerDebugMonoComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				o.HideDebugIcon();
			}
		}
	}
}