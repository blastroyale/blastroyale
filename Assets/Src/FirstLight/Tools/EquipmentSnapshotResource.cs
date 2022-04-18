using UnityEngine;

namespace Src.FirstLight.Tools
{
	[CreateAssetMenu(fileName = "EquipmentSnapshotResource", menuName = "ScriptableObjects/EquipmentSnapshotResource", order = 1)]
	public class EquipmentSnapshotResource : ScriptableObject
	{
		public CategoryPrefabData[] Categories;
	}
}