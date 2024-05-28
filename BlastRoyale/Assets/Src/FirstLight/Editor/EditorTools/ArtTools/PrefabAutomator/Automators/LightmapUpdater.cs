using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class LightUpdater : AutoPrefabUpdater
	{
		public bool CastShadows;
		public bool ReceiveShadows;
			
		[MenuItem("FLG/Art/Prefab Automation/Update Baked Lights")]
		public static void OpenWindow()
		{
			LightUpdater wnd = GetWindow<LightUpdater>();
			wnd.titleContent = new GUIContent("Light Updater");
		}

		protected override void OnRenderUI()
		{
			GUILayout.Label("Light Updater", EditorStyles.boldLabel);
			CastShadows = EditorGUILayout.Toggle("Cast Shadows", CastShadows);
			ReceiveShadows = EditorGUILayout.Toggle("Receive Shadows", ReceiveShadows);
		}
		
		protected override bool OnUpdateGameObject(GameObject o)
		{
			var renderers = o.GetComponentsInChildren<MeshRenderer>(true);
			if (renderers == null || renderers.Length == 0) return false;

			var flags = GameObjectUtility.GetStaticEditorFlags(o);
			if (ReceiveShadows) flags |= StaticEditorFlags.ContributeGI;
			else flags &= ~StaticEditorFlags.ContributeGI;
			GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.ContributeGI);
			
			foreach (var renderer in renderers)
			{
				renderer.shadowCastingMode = CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
				renderer.receiveGI = ReceiveGI.Lightmaps;
			}
			return true;
		}
	}
}