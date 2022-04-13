using System;
using Src.FirstLight.Modules.Services.Runtime.Tools;
using UnityEngine;

namespace Src.FirstLight.Modules.Services.Runtime
{
	
	[CreateAssetMenu(fileName = "EquipmentSnapshotResource", menuName = "ScriptableObjects/EquipmentSnapshotResource", order = 1)]
	public class EquipmentSnapshotResource : ScriptableObject
	{
		public CategoryPrefabData[] Categories;
	}
}