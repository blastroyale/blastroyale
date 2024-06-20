using System;
using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EditorOnly;
using Quantum;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Editor.EditorTools.MapTools
{
	public class QuantumNavmeshColliderImporter : MapDataBakerCallback
	{
		public override void OnBeforeBake(MapData data, MapDataBaker.BuildTrigger buildTrigger, QuantumMapDataBakeFlags bakeFlags)
		{
			// This is called before quantum starts building the navmesh, is it called even before it triggers the unity navmesh builder
			if (bakeFlags.HasFlag(QuantumMapDataBakeFlags.BakeUnityNavMesh))
			{
				EditorUtility.DisplayDialog("Failed!!!", "Baking navmesh through quantum map is not supported!", "Ok");
				throw new Exception("Baking navmesh through quantum map is not supported!");
			}
		}


		public override void OnCollectNavMeshes(MapData data, List<NavMesh> navmeshes)
		{
		}

		public override void OnBeforeBake(MapData data)
		{
		}

		public override void OnBake(MapData data)
		{
		}
	}
}