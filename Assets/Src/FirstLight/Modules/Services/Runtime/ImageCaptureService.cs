using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Src.FirstLight.Modules.Services.Runtime
{
	[Serializable]
	public struct CaptureSnaphotData
	{
		public int category;
		public int subCategory;
		public CaptureSnapshotResource PrefabResource;
	}

	[Serializable]
	public struct CaptureData
	{
		public CaptureSnaphotData[] PrefabCaptureData;
	}

	[Serializable]
	public struct DataBinding
	{
		public string Source;
		public string Target;
	}

	[Serializable]
	public struct StringDataWeighted
	{
		public string Id;
		[Range(0, 1)] public float Weight; //0-1
	}

	[Serializable]
	public struct ColorInterpolation
	{
		public Color Color0;
		public Color Color1;
	}

	[Serializable]
	public struct VectorInterpolation
	{
		public Vector3 Vector0;
		public Vector3 Vector1;
	}

	[Serializable]
	public struct StringDataWeightedColorInterpolation
	{
		public StringDataWeighted StringDataWeighted;
		public ColorInterpolation Value;
	}

	[Serializable]
	public struct StringDataWeightedColor
	{
		public StringDataWeighted StringDataWeighted;
		public Color Value;
	}

	[Serializable]
	public struct StringDataWeightedVector
	{
		public StringDataWeighted StringDataWeighted;
		public Vector3 Value;
	}

	[Serializable]
	public struct StringDataVectorBinding
	{
		public DataBinding Binding;
		public Vector3 Value;
	}

	[Serializable]
	public struct StringDataColorBinding
	{
		public DataBinding Binding;
		public Color Value;
	}

	[Serializable]
	public struct MaterialFloatAttribute
	{
		public string PropertyId;
		public float Value;
	}

	[Serializable]
	public struct MaterialFloatRangeAttribute
	{
		public string PropertyId;
		public float MinimumValue;
		public float MaximumValue;
	}

	[Serializable]
	public struct MaterialIntAttribute
	{
		public string PropertyId;
		public int Value;
	}

	[Serializable]
	public struct MaterialVector3Attribute
	{
		public string PropertyId;
		public Vector3 Value;
	}

	[Serializable]
	public struct MaterialColorAttribute
	{
		public string PropertyId;
		public Color Value;
	}

	[Serializable]
	public struct MaterialVectorRangeAttribute
	{
		public string PropertyId;
		public Vector3 MinimumValue;
		public Vector3 MaximumValue;
	}

	[Serializable]
	public struct MaterialTextureAttribute
	{
		public string PropertyId;
		public Texture Value;
	}

	[Serializable]
	public struct StringDataFloatBinding
	{
		public DataBinding Binding;
		public float Value;
	}

	[Serializable]
	public struct StringDataBoolBinding
	{
		public DataBinding Binding;
		public bool Value;
	}

	[Serializable]
	public struct StringDataGameObjectBinding
	{
		public DataBinding Binding;
		public GameObject Value;
	}

	[Serializable]
	public struct StringDataMaterialBinding
	{
		public DataBinding Binding;
		public Material Value;
		public MaterialTextureAttribute[] MaterialTextureAttributes;
		public MaterialColorAttribute[] MaterialColorAttributes;
		public MaterialVector3Attribute[] MaterialVector3Attributes;
		public MaterialFloatAttribute[] MaterialFloatAttributes;
		public MaterialFloatRangeAttribute[] MaterialFloatRangeAttributes;
		public MaterialIntAttribute[] MaterialIntAttributes;
	}

	
	public class ImageCaptureService : MonoBehaviour
	{
		[SerializeField] private Transform _markerTransform;
		[SerializeField] private RenderTexture _renderTexture;
		[SerializeField] private Camera _camera;
		[SerializeField] private CaptureData _captureData;

		
		public void Run()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			var exportDir = Path.GetDirectoryName(Application.dataPath) + '/' + "Assets/Captures";

			if (Directory.Exists(exportDir))
			{
				Debug.Log($"Deleting directory {exportDir}");
				Directory.Delete(exportDir, true);
			}

			Directory.CreateDirectory(exportDir);

			for (int i = 0; i < _captureData.PrefabCaptureData.Length; i++)
			{
				var prefabCaptureData = _captureData.PrefabCaptureData[i];

				var prefabResource = prefabCaptureData.PrefabResource;

				var go = Instantiate(prefabResource.OwnerPrefab);
				go.transform.SetParent(_markerTransform);
				go.transform.localScale = Vector3.one;

				var bounds = GetBounds(go);
				go.transform.localPosition = -bounds.center;

				for (var j = 0; j < prefabResource.Snapshots.Length; j++)
				{
					var snapshot = prefabResource.Snapshots[i];
					
					for (int k = 0; k < snapshot.GameObjectMaterialBindings.Length; k++)
					{
						var stringDataMaterialBinding = snapshot.GameObjectMaterialBindings[j];

						var childTransform = go.transform.Find(stringDataMaterialBinding.Binding.Target);
						Debug.Assert(childTransform != null,
						             $"Could not locate child transform {go.name} {stringDataMaterialBinding.Binding.Target}");
						var childRenderer = childTransform.gameObject.GetComponent<Renderer>();
						Debug.Assert(childRenderer != null,
						             $"Could not locate child renderer {go.name} {stringDataMaterialBinding.Binding.Target}");

						var material = new Material(stringDataMaterialBinding.Value);

						for (var u = 0; u < stringDataMaterialBinding.MaterialColorAttributes.Length; u++)
						{
							var attribute = stringDataMaterialBinding.MaterialColorAttributes[u];
							material.SetColor(Shader.PropertyToID(attribute.PropertyId), attribute.Value);
						}

						for (var u = 0; u < stringDataMaterialBinding.MaterialFloatAttributes.Length; u++)
						{
							var attribute = stringDataMaterialBinding.MaterialFloatAttributes[u];
							material.SetFloat(attribute.PropertyId, attribute.Value);
						}

						for (var u = 0; u < stringDataMaterialBinding.MaterialIntAttributes.Length; u++)
						{
							var attribute = stringDataMaterialBinding.MaterialIntAttributes[u];
							material.SetInt(attribute.PropertyId, attribute.Value);
						}

						for (var u = 0; u < stringDataMaterialBinding.MaterialVector3Attributes.Length; u++)
						{
							var attribute = stringDataMaterialBinding.MaterialVector3Attributes[u];
							material.SetVector(attribute.PropertyId, attribute.Value);
						}

						childRenderer.materials = new[] { material };
					}

					RenderToTextureCapture($"{prefabCaptureData.category}_{prefabCaptureData.subCategory}_{j}");
				}

				Debug.Log($"[ Exporting capture image {go.name} ]");
				DestroyImmediate(go);
			}
		}

		public void SnapShot()
		{
			RenderToTextureCapture("snapshot");
		}

		private void RenderToTextureCapture(string filename)
		{
			_camera.targetTexture = _renderTexture;

			RenderTexture.active = _renderTexture;

			_camera.Render();

			Texture2D image = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height);
			image.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0);
			image.Apply();
			RenderTexture.active = null;

			byte[] bytes = image.EncodeToPNG();
			DestroyImmediate(image);

			var path = Path.GetDirectoryName(Application.dataPath) + "/Assets/Captures/" + filename + ".png";
			File.WriteAllBytes(path, bytes);
		}
		
		private Bounds GetBounds(GameObject go)
		{
			Bounds bounds = new Bounds();

			Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
			if (renderers.Length > 0)
			{
				foreach (Renderer renderer in renderers)
				{
					if (renderer.enabled)
					{
						bounds = renderer.bounds;
						break;
					}
				}
				
				foreach (Renderer renderer in renderers)
				{
					if (renderer.enabled)
					{
						bounds.Encapsulate(renderer.bounds);
					}
				}
			}

			return bounds;
		}
	}
}