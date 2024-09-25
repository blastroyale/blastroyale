using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class NavmeshQuantumCollidersBaking : MonoBehaviour, ISelfValidator
	{
		[Serializable]
		public class GroupConfig
		{
			public string Name;
			[ChildGameObjectsOnly] public Transform Destination;
			public Transform[] SourceTransform;
			public bool Walkable;
		}

		[SerializeField, FoldoutGroup("Config", expanded: false)]
		private GroupConfig[] _groups;

		[SerializeField, FoldoutGroup("Config", expanded: false)]
		private NavMeshCleaner _cleaner;

		[SerializeField, HideInInspector] private Transform _floorObj;

		public void Validate(SelfValidationResult result)
		{
#if UNITY_EDITOR
			foreach (var groupConfig in _groups)
			{
				if (groupConfig.SourceTransform.Length == 0)
				{
					result.AddError("No source colliders configured for group " + groupConfig.Name + "!");
				}
			}

			if ((_cleaner.m_WalkablePoint?.Count ?? 0) == 0)
			{
				result.AddWarning("There is no walkable points configured in the Cleaner script!\nCleaner will do nothing!");
			}
#endif
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			_cleaner ??= GetComponentInChildren<NavMeshCleaner>();
		}

		[FoldoutGroup("Debug", expanded: false)]
		[GUIColor(0.21f, 0.65f, 0.45f)]
		[Button(ButtonSizes.Small, Name = "Create Colliders", Icon = SdfIconType.PlusCircle)]
		public void CreateColliders()
		{
			CreateFloor();
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
					CreateCollidersBasedOn(source, groupConfig.Destination, groupConfig.Walkable);
				}
			}
		}

		[FoldoutGroup("Debug", expanded: false)]
		[GUIColor(0.65f, 0.16f, 0.18f)]
		[Button(ButtonSizes.Small, Name = "Delete Colliders", Icon = SdfIconType.DashCircle)]
		public void DeleteColliders()
		{
			if (_floorObj != null)
			{
				DestroyImmediate(_floorObj.gameObject);
			}

			var children = new List<GameObject>();
			foreach (var groupConfig in _groups)
			{
				foreach (Transform child in groupConfig.Destination)
				{
					children.Add(child.gameObject);
				}
			}

			children.ForEach(DestroyImmediate);
		}

		[FoldoutGroup("Debug", expanded: false)]
		[GUIColor(0.21f, 0.65f, 0.45f)]
		[Button(ButtonSizes.Small, Name = "Apply Cleaner", Icon = SdfIconType.PlusSquare)]
		private void ApplyCleaner()
		{
			_cleaner.Build();
			_cleaner.SetMeshVisible(true);
			foreach (var o in _cleaner.m_Child)
			{
				var newCollider = o.AddComponent<MeshCollider>();
				newCollider.sharedMesh = o.GetComponent<MeshFilter>().sharedMesh;
			}
		}

		[FoldoutGroup("Debug", expanded: false)]
		[GUIColor(0.65f, 0.16f, 0.18f)]
		[Button(ButtonSizes.Small, Name = "Reset Cleaner", Icon = SdfIconType.DashSquare)]
		private void ResetCleaner()
		{
			_cleaner.Reset();
		}

		private void CreateCollidersBasedOn(Transform parentSource, Transform parentDestination, bool walkable)
		{
			var colliders = parentSource.GetComponentsInChildren<QuantumStaticBoxCollider2D>();
			foreach (var boxCollider2D in colliders)
			{
				var go = new GameObject("TempCollider " + boxCollider2D.gameObject.name, typeof(BoxCollider), typeof(NavMeshModifierVolume));
				var goPosition = boxCollider2D.transform.position;
				goPosition.y = 0;
				var goScale = boxCollider2D.transform.localScale;
				goScale.y = 5;
				go.transform.position = goPosition;
				go.transform.rotation = Quaternion.Euler(boxCollider2D.transform.rotation.eulerAngles);
				go.transform.localScale = goScale;
				var bx = go.GetComponent<BoxCollider>();
				var colliderSize = boxCollider2D.Size.ToUnityVector3();
				colliderSize.y = 5;
				bx.center = boxCollider2D.PositionOffset.ToUnityVector3();
				bx.size = colliderSize;
				var modifierVolume = go.GetComponent<NavMeshModifierVolume>();
				modifierVolume.center = bx.center;
				modifierVolume.size = bx.size;
				modifierVolume.area = walkable ? 0 : 1;
				go.transform.SetParent(parentDestination);
			}
		}

		private void CreateFloor()
		{
			if (_floorObj != null)
			{
				DestroyImmediate(_floorObj.gameObject);
			}

			var mapSize = GetComponentInParent<MapData>().Asset.Settings.WorldSize;
			_floorObj = new GameObject("TempColliderFloor", typeof(BoxCollider),typeof(NavMeshModifier)).transform;
			_floorObj.parent = this.transform;
			_floorObj.transform.position = Vector3.zero;
			_floorObj.transform.localScale = new Vector3(mapSize, 0.01f, mapSize);
		}

		private async UniTaskVoid Bake(bool applyCleaner)
		{
			// Bake mesh with islands
			var navmeshSurface = GetComponent<NavMeshSurface>();
			if (Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(navmeshSurface)) return;

			DeleteColliders();
			CreateColliders();
			Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] {navmeshSurface});
			await UniTask.WaitUntil(() => !Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(navmeshSurface));
			if (!applyCleaner || _cleaner.m_WalkablePoint?.Count == 0)
			{
				DeleteColliders();
				return;
			}

			// Now apply the cleaner creating a new mesh collider to remove the islands
			ApplyCleaner();
			Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] {navmeshSurface});
			await UniTask.WaitUntil(() => !Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(navmeshSurface));
			ResetCleaner();
			DeleteColliders();
		}

		[GUIColor(1f, 0f, 1f)]
		[Button(ButtonSizes.Large, Icon = SdfIconType.Hammer, Name = "Bake Navmesh", Style = ButtonStyle.Box, Expanded = true)]
		public void BakeUnityNavMesh(bool applyCleaner = true)
		{
			Bake(applyCleaner).Forget();
		}
#endif
	}
}