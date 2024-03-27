using Photon.Deterministic;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Editor.EditorTools.MapTools
{
	public static class MapBuildingTool
	{
		[MenuItem("FLG/Map/Create GOs with Quantum Colliders From Selected GOs &0")]
		private static void CreateQuantumColliders()
		{
			var parentGo = new GameObject("Building");
			for (var i = 0; i < Selection.transforms.Length; i++)
			{
				var selected = Selection.transforms[i];

				var newGo = new GameObject("Wall COLL", typeof(QuantumStaticBoxCollider3D));
				newGo.transform.position = new Vector3(selected.position.x, 1.5f, selected.position.z);
				newGo.layer = 8;
				newGo.transform.SetParent(parentGo.transform);

				var coll = newGo.GetComponent<QuantumStaticBoxCollider3D>();
				coll.Size = new FPVector3(selected.localScale.x.ToFP(), FP._3, selected.localScale.z.ToFP());
			}
		}

		[MenuItem("FLG/Map/Create GO with a single Quantum Collider at Positions of Selected GOs &9")]
		private static void CreateSingleQuantumCollider()
		{
			for (var i = 0; i < Selection.transforms.Length; i++)
			{
				var selected = Selection.transforms[i];

				var newGo = new GameObject(selected.gameObject.name + " Obstacle COLL", typeof(QuantumStaticBoxCollider3D));
				newGo.transform.position = new Vector3(selected.position.x, 1.5f, selected.position.z);
				newGo.layer = 8;

				var coll = newGo.GetComponent<QuantumStaticBoxCollider3D>();
				coll.Size = new FPVector3(FP._1, FP._3, FP._1);
			}
		}

		[MenuItem("FLG/Map/PrefabItAll")]
		private static void PrefabItAll()
		{
			//GameObjectHashSet done = new GameObjectHashSet();
			var done = new HashSet<string>();
			foreach (var o in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (PrefabUtility.IsPartOfRegularPrefab(o))
				{
					var root = PrefabUtility.GetOutermostPrefabInstanceRoot(o);
					if (done.Contains(root.name)) continue;

					EditorUtility.SetDirty(o);
					PrefabUtility.RecordPrefabInstancePropertyModifications(o);
					PrefabUtility.ApplyPrefabInstance(o, InteractionMode.AutomatedAction);
					done.Add(root.name);
				}
			}
		}

		[MenuItem("FLG/Map/OptimizeLightmaps")]
		private static void OptimizeLightmaps()
		{
			var lit = Shader.Find("Universal Render Pipeline/Simple Lit");
			var lambert = Shader.Find("FLG/FastLambertShader");
			var baked = Shader.Find("FLG/Baked/Static Object");
			var unlit = Shader.Find("Unlit/Color");

			var quad = GameObject.Find("USE_THIS").GetComponent<MeshFilter>().sharedMesh;

			Debug.Log("Running");
			foreach (var o in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				var rend = o.GetComponent<MeshRenderer>();
				if (rend == null) continue;

				var filter = o.GetComponent<MeshFilter>();
				if (filter == null) continue;

				Debug.Log("Checking " + o.name + " with shader " + rend.sharedMaterial.shader.name);

				var rightShader = rend.sharedMaterial.shader.name.Contains("Static Object") || rend.sharedMaterial.shader == lambert;
				var isFloor = o.transform.position.y < -0.01 && (filter.sharedMesh.name.ToLower().Contains("cube") || filter.sharedMesh.name.ToLower().Contains("quad"));
				if (!isFloor)
				{
					Debug.Log("Updating object " + o.name);
					rend.sharedMaterial.shader = lambert;
					rend.receiveShadows = false;
					rend.scaleInLightmap = 0;
				}
				else if (isFloor)
				{
					filter.sharedMesh = quad;
					filter.transform.position += Vector3.up;
				}
				else
				{
					Debug.Log("RightShader ? " + rightShader + " isFloor ? " + isFloor);
				}

			}
		}

		[MenuItem("FLG/Map/Disable Non Lightmapped")]
		private static void DisableNonLightmapped()
		{

			foreach (var o in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				var rend = o.GetComponent<MeshRenderer>();
				if (rend == null) continue;
				var filter = o.GetComponent<MeshFilter>();
				if (filter == null) continue;

				if (rend.receiveGI == ReceiveGI.LightProbes)
				{
					o.SetActive(true);
				}
			}
		}
	}
}