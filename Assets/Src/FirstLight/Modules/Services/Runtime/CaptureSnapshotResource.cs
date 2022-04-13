using System;
using Src.FirstLight.Modules.Services.Runtime.Tools;
using UnityEngine;

namespace Src.FirstLight.Modules.Services.Runtime
{
	[Serializable]
	public struct CaptureSnapshot
	{
		public StringDataBoolBinding[] GameObjectOnOffBindings;
		public StringDataGameObjectBinding[] GameObjectPartBindings;
		public StringDataVectorBinding[] GameObjectScaleBindings;
		public StringDataMaterialBinding[] GameObjectMaterialBindings;
	}

	[CreateAssetMenu(fileName = "CaptureSnapshotResource", menuName = "ScriptableObjects/CaptureSnapshotResource", order = 1)]
	public class CaptureSnapshotResource : ScriptableObject
	{
		public GameObject OwnerPrefab;
		public CaptureSnapshot[] Snapshots;
	}
}