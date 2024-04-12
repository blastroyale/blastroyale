using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class NavmeshQuantumCollidersBaking : MonoBehaviour
	{
		[Serializable]
		public class GroupConfig
		{
			public string Name;
			public Transform[] SourceTransform;
			public Transform Destination;
		}

		[SerializeField] private GroupConfig[] _groups;


		#if UNITY_EDITOR
		[Button(ButtonSizes.Medium)]
		public void CreateColliders()
		{
			foreach (var groupConfig in _groups)
			{
				// Already created
				if (groupConfig.Destination.transform.childCount > 0)
				{
					Debug.LogError($"Colliders already created for group {groupConfig.Name}!");
					return;
				}

				foreach (var source in groupConfig.SourceTransform)
				{
					CreateCollidersBasedOn(source, groupConfig.Destination);
				}
			}
		}

		private void CreateCollidersBasedOn(Transform parentSource, Transform parentDestination)
		{
			var colliders = parentSource.GetComponentsInChildren<QuantumStaticBoxCollider3D>();
			foreach (var q3d in colliders)
			{
				var go = new GameObject("TempCollider " + q3d.gameObject.name, typeof(BoxCollider));
				go.transform.position = q3d.transform.position;
				go.transform.rotation = Quaternion.Euler(q3d.transform.rotation.eulerAngles + q3d.RotationOffset.ToUnityVector3());
				var bx = go.GetComponent<BoxCollider>();
				bx.center = q3d.PositionOffset.ToUnityVector3();
				bx.size = q3d.Size.ToUnityVector3();
				go.transform.SetParent(parentDestination);
			}
		}

		[Button(ButtonSizes.Medium)]
		public void DeleteColliders()
		{
			foreach (var groupConfig in _groups)
			{
				var children = new List<GameObject>();
				foreach (Transform child in groupConfig.Destination)
				{
					children.Add(child.gameObject);
				}

				children.ForEach(DestroyImmediate);
			}
		}
		#endif
	}
}