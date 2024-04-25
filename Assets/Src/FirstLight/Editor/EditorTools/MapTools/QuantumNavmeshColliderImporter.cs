using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EditorOnly;
using Quantum;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.MapTools
{
	public class QuantumNavmeshColliderImporter : MapDataBakerCallback
	{
		public override void OnBeforeBake(MapData data, MapDataBaker.BuildTrigger buildTrigger, QuantumMapDataBakeFlags bakeFlags)
		{
			// This is called before quantum starts building the navmesh, is it called even before it triggers the unity navmesh builder
			// So I create the Unity colliders here and delete it after we don't need it anymore
			Debug.Log("[FLGMap] Creating UnityColliders before baking quantum map!");
			if (!bakeFlags.HasFlag(QuantumMapDataBakeFlags.BakeUnityNavMesh)) return;
			var baking = Object.FindObjectOfType<NavmeshQuantumCollidersBaking>();
			if (baking == null) return;
			baking.DeleteColliders();
			baking.CreateColliders();
		}


		public override void OnCollectNavMeshes(MapData data, List<NavMesh> navmeshes)
		{
			// When this method is called, the navmeshes are already generated, so we can delete the colliders
			Debug.Log("[FLGMap] Delete UnityColliders after generating navmesh!");
			var baking = Object.FindObjectOfType<NavmeshQuantumCollidersBaking>();
			if (baking == null) return;
			baking.DeleteColliders();
		}

		public override void OnBeforeBake(MapData data)
		{
		}

		public override void OnBake(MapData data)
		{
		}
	}
}