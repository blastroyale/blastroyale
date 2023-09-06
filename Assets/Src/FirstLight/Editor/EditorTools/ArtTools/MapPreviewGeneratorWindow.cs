using System;
using System.IO;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace FirstLight.Editor.EditorTools.ArtTools
{
	/// <summary>
	/// Art tool to generate PNG image of map preview
	/// </summary>
	public class MapPreviewGeneratorWindow : OdinEditorWindow
	{
		[SerializeField, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		private string _outputDirectory;

		[SerializeField, Required]
		private GameObject _mapPreviewSetup;
		
		[SerializeField, Required]
		private RenderTexture _renderTexture;

		private Camera _camera;
		
		
		[MenuItem("FLG/Art/Generators/Map Preview")]
		private static void OpenWindow()
		{
			GetWindow<MapPreviewGeneratorWindow>("Map Preview Generator").Show();
		}
		
		[Button, HideInEditorMode]
		private void PrepareSnapshot()
		{
			if (_camera != null)
			{
				Destroy(_camera.gameObject);
				_camera = null;
			}

			var go = GameObject.Instantiate(_mapPreviewSetup);
			_camera = go.GetComponentInChildren<Camera>();
		}

		[Button, HideInEditorMode]
		private void ExportSnapshotImage()
		{
			var bounds = CalculateAllGeometryBounds();
			
			float cameraSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
			_camera.orthographicSize = cameraSize;
			
			Vector3 boundsCenter = bounds.center; 
			float distance = CalculateCameraDistance(bounds, _camera, 1f); 
			Vector3 cameraPosition = boundsCenter - _camera.transform.forward * distance;

			_camera.transform.position = cameraPosition.x * _camera.transform.right 
				+ cameraPosition.z *  _camera.transform.up
				+ cameraPosition.y * -_camera.transform.forward;
			
			WriteRenderTextureToDisk(_camera, SceneManager.GetActiveScene().name);
		}
		
		private float CalculateCameraDistance(Bounds bounds, Camera camera, float zoomFactor)
		{
			float boundsSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
			return boundsSize / (2.0f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) * zoomFactor;
		}

		private void HideSceneGeometry()
		{
			GameObject[] allObjects = FindObjectsOfType<GameObject>();
			
			foreach (GameObject obj in allObjects)
			{
				Renderer renderer = obj.GetComponent<Renderer>();
				if (renderer != null)
				{
					renderer.enabled = false;
				}
			}
		}

		private void ShowSceneGeometry()
		{
			GameObject[] allObjects = GameObject.FindGameObjectsWithTag("MapPreviewObject");
			foreach (GameObject obj in allObjects)
			{
				Renderer renderer = obj.GetComponent<Renderer>();
				if (renderer != null)
				{
					renderer.enabled = true;
				}
			}
		}

		private Bounds CalculateAllGeometryBounds()
		{
			var sceneBounds = new Bounds();
			var foundBounds = false;
			GameObject[] allObjects = GameObject.FindGameObjectsWithTag("MapPreviewObject"); 
			
			foreach (GameObject obj in allObjects)
			{
				Renderer renderer = obj.GetComponent<Renderer>();
				if (renderer != null && obj.activeSelf && renderer.enabled)
				{
					if (!foundBounds)
					{
						sceneBounds = renderer.bounds;
						
						foundBounds = true;
					}
					else
					{
						sceneBounds.Encapsulate(renderer.bounds);	
					}
				}
			}

			return sceneBounds;
		}
		
		
		[UsedImplicitly]
		private bool IsEnvironmentReady()
		{
			return Application.isPlaying;
		}
		
		private void WriteRenderTextureToDisk(Camera camera, string filename, bool crop = false)
		{
			var ext = ".png";
			var path = Path.Combine(_outputDirectory, filename + ext);

			HideSceneGeometry();
			ShowSceneGeometry();
			
			RenderTexture.active = _renderTexture;
			camera.targetTexture = _renderTexture;
			RenderTexture.active = _renderTexture;

			camera.Render();
			
			var width = _renderTexture.width;
			var height = _renderTexture.height;

			var image = new Texture2D(width, height);
			image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			image.Apply();

			if (crop)
			{
				var pixels = image.GetPixels(0, 0, width, height);

				var minCroppedWidth = -1;
				var maxCroppedWidth = -1;

				for (var i = 0; i < height; i++)
				{
					for (var j = 0; j < width; j++)
					{
						if (pixels[j + (i * width)].a > 0)
						{
							if (j < minCroppedWidth || minCroppedWidth == -1)
							{
								minCroppedWidth = j;
							}
						}

						var index = (width - 1) - j;
						if (pixels[index + (i * width)].a > 0)
						{
							if (index > maxCroppedWidth || maxCroppedWidth == -1)
							{
								maxCroppedWidth = index;
							}
						}
					}
				}

				var minCroppedHeight = -1;
				var maxCroppedHeight = -1;

				for (var i = 0; i < width; i++)
				{
					for (var j = 0; j < height; j++)
					{
						if (pixels[i + (j * width)].a > 0)
						{
							if (j < minCroppedHeight || minCroppedHeight == -1)
							{
								minCroppedHeight = j;
							}
						}

						var index = (height - 1) - j;

						if (pixels[i + (index * width)].a > 0)
						{
							if (index > maxCroppedHeight || maxCroppedHeight == -1)
							{
								maxCroppedHeight = index;
							}
						}
					}
				}

				maxCroppedHeight = image.height - maxCroppedHeight;
				minCroppedHeight = image.height - minCroppedHeight;

				var copyWidth = Math.Abs(maxCroppedWidth - minCroppedWidth);
				var copyHeight = Math.Abs(maxCroppedHeight - minCroppedHeight);

				var copyTexture = new Texture2D(copyWidth, copyHeight);
				copyTexture.ReadPixels(new Rect(minCroppedWidth, maxCroppedHeight, copyWidth, copyHeight), 0, 0);
				copyTexture.Apply();

				RenderTexture.active = null;

				var bytes = copyTexture.EncodeToPNG();

				DestroyImmediate(copyTexture);
				DestroyImmediate(image);

				File.WriteAllBytes(path, bytes);
			}
			else
			{
				var bytes = image.EncodeToPNG();

				DestroyImmediate(image);

				File.WriteAllBytes(path, bytes);
			}
		}
	}
}