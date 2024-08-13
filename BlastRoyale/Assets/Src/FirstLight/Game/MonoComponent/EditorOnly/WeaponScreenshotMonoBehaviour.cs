using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Editor.EditorTools.Skins
{
	public class WeaponScreenshot : MonoBehaviour
	{
#if UNITY_EDITOR
		public IEnumerator<Pair<GameId, GameObject>> _skins;
		private Camera _camera;
		private Vector3 _originalCameraPos;
		private Quaternion _originalCameraRot;
		
		private RenderTexture _rt;

		private async void Start()
		{
			_camera = Camera.main;
			_originalCameraPos = _camera.transform.position;
			_originalCameraRot = _camera.transform.rotation;
			_rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			_camera.targetTexture = _rt;
			var id = AddressableConfigLookup.GetConfig(AddressableId.Collections_WeaponSkins_Config);
			var config = await Addressables.LoadAssetAsync<WeaponSkinsConfigContainer>(id.Address).Task;

			var hammerGroup = config.Config.MeleeWeapons;
			var skins = new List<Pair<GameId, GameObject>>();
			foreach ( var kp in hammerGroup)
			{
				var key = kp.Key;
				var value = kp.Value;
				if (value.Prefab == null) continue;
				skins.Add(new Pair<GameId, GameObject>(key, await Addressables.LoadAssetAsync<GameObject>(value.Prefab).Task));
			}

			_skins = skins.GetEnumerator();
			StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			var id = AddressableConfigLookup.GetConfig(AddressableId.Collections_WeaponSkins_Config);
			var op = Addressables.LoadAssetAsync<WeaponSkinsConfigContainer>(id.Address);

			yield return new WaitUntil(() => op.IsDone);

			var config = op.Result;

			while (_skins.MoveNext())
			{
				var current = _skins.Current;
				var screenShotFile = Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(current.Value)) + "/sprite_automatic.png";
				for (int i = transform.childCount - 1; i >= 0; i--)
				{
					Destroy(transform.GetChild(i).gameObject);
				}


				var obj = Instantiate(current.Value, transform);
				yield return new WaitForSeconds(0.1f);
				FocusOn(_camera, obj, 1.1f);
				var sprite = TakeScreenshot(screenShotFile);
				
				_camera.transform.SetPositionAndRotation(_originalCameraPos, _originalCameraRot);
				var currentEntry = config.Config.MeleeWeapons.Get(current.Key);
				if (currentEntry.Sprite == null || currentEntry.Sprite.editorAsset == null)
				{
					Debug.Log("Setting sprite " + current.Key);
					currentEntry.Sprite = sprite;
				}
			}

			UnityEditor.EditorUtility.SetDirty(config);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();

			UnityEditor.EditorApplication.isPlaying = false;
		}

		public static Bounds GetBoundsWithChildren(GameObject gameObject)
		{
			Renderer parentRenderer = gameObject.GetComponent<Renderer>();

			Renderer[] childrenRenderers = gameObject.GetComponentsInChildren<Renderer>();

			Bounds bounds = parentRenderer != null
				? parentRenderer.bounds
				: childrenRenderers.FirstOrDefault(x => x.enabled).bounds;

			if (childrenRenderers.Length > 0)
			{
				foreach (Renderer renderer in childrenRenderers)
				{
					if (renderer.enabled)
					{
						bounds.Encapsulate(renderer.bounds);
					}
				}
			}

			return bounds;
		}

		public static void FocusOn(Camera c, GameObject focusedObject, float marginPercentage)
		{
			Bounds b = GetBoundsWithChildren(focusedObject);
			Vector3 max = b.size;
			float radius = Mathf.Max(max.x, Mathf.Max(max.y, max.z));
			float dist = radius / (Mathf.Sin(c.fieldOfView * Mathf.Deg2Rad / 2f));
			Debug.Log("Radius = " + radius + " dist = " + dist);

			Vector3 view_direction = c.transform.InverseTransformDirection(Vector3.forward);

			Vector3 pos = view_direction * dist + b.center;
			c.transform.position = pos;
			c.transform.LookAt(b.center);
		}

		private AssetReferenceSprite TakeScreenshot(string path)
		{
			_rt.Release();
			_camera.Render();

			RenderTexture.active = _rt;
			Texture2D tex = new Texture2D(_rt.width, _rt.height, TextureFormat.ARGB32, false);
			tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
			RenderTexture.active = null;

			byte[] bytes = tex.EncodeToPNG();
			System.IO.File.WriteAllBytes(path, bytes);
			UnityEditor.AssetDatabase.ImportAsset(path);
			UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
			importer.textureType = UnityEditor.TextureImporterType.Sprite;
			Debug.Log("Saved to " + path);
			UnityEditor.AssetDatabase.WriteImportSettingsIfDirty(path);
			var newPrefabGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
			return new AssetReferenceSprite(newPrefabGuid);
		}
#endif
	}
}