using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Src.FirstLight.Modules.Services.Runtime.Tools
{
	public interface IErcRenderable
	{
		public void Initialise(Erc721Metadata metadata);
	}
	
	public struct TraitAttribute
	{
		public string trait_type;
		public int value;
	}
	
	public struct Erc721Metadata 
	{
		public string name;
		public string description;
		public string image;
		public TraitAttribute[] attributes;
		public Dictionary<string, int> attibutesDictionary;
		
		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			attibutesDictionary = new Dictionary<string, int>(attributes.Length);

			for (var i=0; i<attributes.Length; i++)
			{
				attibutesDictionary.Add(attributes[i].trait_type, attributes[i].value);
			}
		}
	}
	
	[Serializable]
	public struct CaptureSnaphotData
	{
		public int category;
		public int subCategory;
		public CaptureSnapshotResource PrefabResource;
	}
	
	[Serializable]
	public struct GameObjectDimensional
	{
		public GameObject[] GameObjects;
	}
	
	[Serializable]
	public struct CategoryPrefabData
	{
		public GameObjectDimensional[] ManufacturerPrefabData;
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
		[BoxGroup("Folder Paths")]
		[FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _exportFolderPath;
		[BoxGroup("Folder Paths")]
		[FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _importFolderPath;
		[FilePath(Extensions = "json", RequireExistingPath = true)]
		public string _metadataJsonFilePath;
		[SerializeField] private Transform _markerTransform;
		[SerializeField] private RenderTexture _renderTexture;
		[SerializeField] private Camera _camera;
		[SerializeField] private GameObject _background;
		[SerializeField] private EquipmentSnapshotResource _equipmentSnapshotResource;
		
		
		[Button("Export Metadata Collection")]
		public void ExportMetadataCollection()
		{
			if (_exportFolderPath == "" || !Directory.Exists(_exportFolderPath))
			{
				Debug.LogError($"Invalid export folder path [{_exportFolderPath}]");
				
				return;
			}
			
			if (_importFolderPath == "" || !Directory.Exists(_importFolderPath))
			{
				Debug.LogError($"Invalid import folder path [{_importFolderPath}]");
				
				return;
			}
			
			var backgroundErcRenderable = _background.GetComponent<IErcRenderable>();

			var fileCount = 0;
			foreach (var filePath in Directory.EnumerateFiles(_importFolderPath, "*.json"))
			{
				Export(filePath, backgroundErcRenderable);
				fileCount++;
			}
			
			Debug.Log($"Loaded [{fileCount} metadata files]");
		}

		[Button("Export Metadata Json")]
		public void ExportMetadataJson()
		{
			if (_exportFolderPath == "" || !Directory.Exists(_exportFolderPath))
			{
				Debug.LogError($"Invalid export folder path [{_exportFolderPath}]");
				
				return;
			}
			
			if (_metadataJsonFilePath == "" || !File.Exists(_metadataJsonFilePath))
			{
				Debug.LogError($"Invalid json file path [{_metadataJsonFilePath}]");
				
				return;
			}
			
			var backgroundErcRenderable = _background.GetComponent<IErcRenderable>();
			
			Export(_metadataJsonFilePath, backgroundErcRenderable);
		}

		[Button("Snapshot")]
		public void SnapShot()
		{
			if (_exportFolderPath == "" || !Directory.Exists(_exportFolderPath))
			{
				Debug.LogError($"Invalid export path [{_exportFolderPath}]");
				
				return;
			}
			
			RenderToTextureCapture("snapshot");
		}
		
		[Button("Centre Marker Children")]
		public void CentreMarkerChildren()
		{
			for (int i = 0; i < _markerTransform.childCount; i++)
			{
				var child = _markerTransform.GetChild(i).gameObject;
				child.transform.localPosition = Vector3.zero;
				var bounds = GetBounds(child);
				child.transform.localPosition = -bounds.center;
				child.transform.localRotation = Quaternion.identity;

				var max = bounds.size;
				var radius = max.magnitude / 2f;
				var horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad / 2f) * _camera.aspect) * Mathf.Rad2Deg;
				var fov = Mathf.Min(_camera.fieldOfView, horizontalFOV);
				var dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));

				_camera.transform.position = new Vector3(-dist, 0, 0);
			}
		}

		private void Export(string filePath, IErcRenderable backgroundErcRenderable)
		{
			var jsonData = File.ReadAllText(filePath);
			
			var metadata = JsonConvert.DeserializeObject<Erc721Metadata>(jsonData);
			
			Debug.Log($"Loading Erc721Metadata JSON file [{filePath}]");
				
			var manufacturerId = (long)metadata.attributes.FirstOrDefault(a => a.trait_type == "manufacturer").value;
			var categoryId = (long)metadata.attributes.FirstOrDefault(a => a.trait_type == "category").value;
			var subcategoryId = (long)metadata.attributes.FirstOrDefault(a => a.trait_type == "subCategory").value;
			
			var categoryPrefabData = _equipmentSnapshotResource.Categories[categoryId];
			if (categoryPrefabData.ManufacturerPrefabData.Length > manufacturerId &&
			    categoryPrefabData.ManufacturerPrefabData[manufacturerId].GameObjects.Length > subcategoryId)
			{
				var prefabResource = _equipmentSnapshotResource.Categories[categoryId].ManufacturerPrefabData[manufacturerId].GameObjects[subcategoryId];

				var go = Instantiate(prefabResource);
				
				var ercRenderable = go.GetComponent<IErcRenderable>();
				ercRenderable.Initialise(metadata);
				
				go.transform.SetParent(_markerTransform);
				go.transform.localScale = Vector3.one;

				go.transform.localPosition = Vector3.zero;
				var bounds = GetBounds(go);
				go.transform.localPosition = -bounds.center;
				go.transform.localRotation = Quaternion.identity;

				var max = bounds.size;
				var radius = max.magnitude / 2f;
				var horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad / 2f) * _camera.aspect) * Mathf.Rad2Deg;
				var fov = Mathf.Min(_camera.fieldOfView, horizontalFOV);
				var dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));

				_camera.transform.position = new Vector3(-dist, 0, 0);

	
					
				backgroundErcRenderable?.Initialise(metadata);
					
				RenderToTextureCapture(Path.GetFileName(metadata.image));
				
				DestroyImmediate(go);
			}
			else
			{
				backgroundErcRenderable?.Initialise(metadata);
				
				RenderToTextureCapture(Path.GetFileName(metadata.image));
			}
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
			_camera.targetTexture = null;
			
			byte[] bytes = image.EncodeToPNG();
			DestroyImmediate(image);
			
			var path = Path.Combine(_exportFolderPath,filename + ".png");
			Debug.Log($"[ Exporting capture image {path} ]");
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
					if (renderer.enabled && gameObject.activeSelf)
					{
						bounds = renderer.bounds;
						
						break;
					}
				}
				
				foreach (Renderer renderer in renderers)
				{
					if (renderer.enabled && gameObject.activeSelf)
					{
						bounds.Encapsulate(renderer.bounds);
					}
				}
			}

			return bounds;
		}
	}
}