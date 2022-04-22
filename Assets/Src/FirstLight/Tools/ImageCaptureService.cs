using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FirstLight.Game.Data;
using FirstLight.Game.MonoComponent;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Src.FirstLight.Tools
{
	public struct IntFloatKeyValue
	{
		public int Key;
		public float Value;
	}
	
	public struct StringIntKeyValue
	{
		public string Key;
		public int Value;
	}
	
	public struct NftEquipmentAttributes
	{
		public string EquipmentMetadataDescription;
		public StringIntKeyValue[] AttributeIntTypes;
		public string[][] SubCategoryNames;
		public IntFloatKeyValue[] CategoryValues;
		public IntFloatKeyValue[][] SubCategoryValues;
		public IntFloatKeyValue[] AdjectiveValues;
		public IntFloatKeyValue[] RarityValues;
		public IntFloatKeyValue[] MaterialValues;
		public IntFloatKeyValue[] ManufacturerValues;
		public IntFloatKeyValue[] FactionValues;
		public IntFloatKeyValue[] GradeValues;
		public int MinDurability;
		public int MaxDurability;
		public float[] MaxLevelValues;
		public int InitialReplicationCounter;
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
		private enum RenderTextureMode
		{
			Standard,
			Standalone,
			Both
		}

		[BoxGroup("Folder Paths")]
		[FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _importFolderPath;
		[BoxGroup("Folder Paths")]
		[FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _exportFolderPath;
		[FilePath(Extensions = "json", RequireExistingPath = true)]
		public string _metadataJsonFilePath;
		[FilePath(Extensions = "json", RequireExistingPath = true)]
		public string _metadataAttributesJsonFilePath;
		[SerializeField] private Transform _markerTransform;
		[SerializeField] private RenderTexture _renderTexture;
		[SerializeField] private RenderTexture _renderTextureStandalone;
		[SerializeField] private Camera _camera;
		[SerializeField] private GameObject _canvasRootGameObject;
		[SerializeField] private EquipmentSnapshotResource _equipmentSnapshotResource;
		[SerializeField] private RenderTextureMode _renderTextureMode;
		
		private const string _webMarketplaceUri = "https://flgmarketplacestorage.z33.web.core.windows.net";
		private NftEquipmentAttributes _nftEquipmentAttributes;

		private readonly Vector2 _referenceResolution = new(1600, 2048);
		
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
			
			var backgroundErcRenderable = _canvasRootGameObject.GetComponent<IErcRenderable>();

			var fileCount = 0;
			foreach (var filePath in Directory.EnumerateFiles(_importFolderPath, "*.json"))
			{
				ExportRenderTargetImage(filePath, backgroundErcRenderable);
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
			
			var backgroundErcRenderable = _canvasRootGameObject.GetComponent<IErcRenderable>();
			
			ExportRenderTargetImage(_metadataJsonFilePath, backgroundErcRenderable);
		}

		
		private void ExportRenderTargetFromMetadata(Erc721MetaData metadata, IErcRenderable backgroundErcRenderable)
		{
			var manufacturerId = metadata.attibutesDictionary["manufacturer"];
			var categoryId = metadata.attibutesDictionary["category"];
			var subcategoryId = metadata.attibutesDictionary["subCategory"];
			

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
				
				_canvasRootGameObject.SetActive(true);
				
				backgroundErcRenderable?.Initialise(metadata);

				if (_renderTextureMode == RenderTextureMode.Standard || _renderTextureMode == RenderTextureMode.Both)
				{
					WriteRenderTextureToDisk(Path.GetFileName(metadata.image), _renderTexture);
				}

				_canvasRootGameObject.SetActive(false);

				if (_renderTextureMode == RenderTextureMode.Standalone || _renderTextureMode == RenderTextureMode.Both)
				{
					WriteRenderTextureToDisk($"{Path.GetFileName(metadata.image)}_standalone", _renderTextureStandalone);
				}

				DestroyImmediate(go);
			}
			else
			{
				_canvasRootGameObject.SetActive(true);
				
				backgroundErcRenderable?.Initialise(metadata);

				if (_renderTextureMode == RenderTextureMode.Standard || _renderTextureMode == RenderTextureMode.Both)
				{
					WriteRenderTextureToDisk(Path.GetFileName(metadata.image), _renderTexture);
				}

				_canvasRootGameObject.SetActive(false);

				if (_renderTextureMode == RenderTextureMode.Standalone || _renderTextureMode == RenderTextureMode.Both)
				{
					WriteRenderTextureToDisk($"{Path.GetFileName(metadata.image)}_standalone", _renderTextureStandalone);
				}
			}
		}

		[Button("Export Image Combinations")]
		private void ExportImageCombinations()
		{
			if (_exportFolderPath == "" || !Directory.Exists(_exportFolderPath))
			{
				Debug.LogError($"Invalid export folder path [{_exportFolderPath}]");
				
				return;
			}
			
			if (_metadataAttributesJsonFilePath == "" || !File.Exists(_metadataAttributesJsonFilePath))
			{
				Debug.LogError($"Invalid meta attributes file path [{_metadataAttributesJsonFilePath}]");
				
				return;
			}
			
			LoadEquipmentAttributesData(_metadataAttributesJsonFilePath);
			
			var backgroundErcRenderable = _canvasRootGameObject.GetComponent<IErcRenderable>();

			var metadata = new Erc721MetaData()
			{
				attributes = new[]
				{
					new TraitAttribute()
					{
						trait_type = "id",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "generation",
						value = 1
					},
					new TraitAttribute()
					{
						trait_type = "edition",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "category",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "subCategory",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "adjective",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "rarity",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "material",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "manufacturer",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "faction",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "grade",
						value = 0
					},
					new TraitAttribute()
					{
						trait_type = "initialReplicationCounter",
						value = _nftEquipmentAttributes.InitialReplicationCounter
					},
					new TraitAttribute()
					{
						trait_type = "maxDurability",
						value = 0,
					},
					new TraitAttribute()
					{
						trait_type = "maxLevel",
						value = 0,
					},
					new TraitAttribute()
					{
						trait_type = "tuning",
						value = 0
					}
				}
			};
			
			metadata.attibutesDictionary = new Dictionary<string, int>(metadata.attributes.Length);
			for (var index = 0; index < metadata.attributes.Length; index++)
			{
				metadata.attibutesDictionary.Add(metadata.attributes[index].trait_type, metadata.attributes[index].value);
			}
								
			var imagesExportedCount = 0;
			
			for (var i = 0; i < _equipmentSnapshotResource.Categories.Length; i++)
			{
				var category = _equipmentSnapshotResource.Categories[i];

				for (var j = 0; j < category.ManufacturerPrefabData.Length; j++)
				{
					var manufacturer = category.ManufacturerPrefabData[j];

					for (var k = 0; k < manufacturer.GameObjects.Length; k++)
					{
						for (var l = 0; l < _nftEquipmentAttributes.RarityValues.Length; l++)
						{
							for (var m = 0; m < _nftEquipmentAttributes.MaterialValues.Length; m++)
							{
								for (var f = 0; f < _nftEquipmentAttributes.FactionValues.Length; f++)
								{
									for (var g = 0; g < _nftEquipmentAttributes.GradeValues.Length; g++)
									{
										metadata.attibutesDictionary["category"] = i;
										metadata.attibutesDictionary["manufacturer"] = j;
										metadata.attibutesDictionary["subCategory"] = k;
										metadata.attibutesDictionary["rarity"] = l;
										metadata.attibutesDictionary["material"] = m;
										metadata.attibutesDictionary["faction"] = f;
										metadata.attibutesDictionary["grade"] = g;
										metadata.name = _nftEquipmentAttributes.SubCategoryNames[i][metadata.attributes[k].value];

										using (var sha256Hash = SHA256.Create())
										{
											var hash = GetHash(sha256Hash, JsonConvert.SerializeObject(metadata));
											metadata.image = $"{_webMarketplaceUri}/nftimages/{hash}.png";
										}
										
										ExportRenderTargetFromMetadata(metadata, backgroundErcRenderable);
										imagesExportedCount++;
									}
								}
							}
						}
					}
				}
			}
			
			Debug.Log($"Exported [{imagesExportedCount}] image combinations");
		}
		
		[Button("Generate Snapshot Image")]
		public void GenerateSnapShotImage()
		{
			if (_exportFolderPath == "" || !Directory.Exists(_exportFolderPath))
			{
				Debug.LogError($"Invalid export path [{_exportFolderPath}]");
				
				return;
			}
			
			_canvasRootGameObject.SetActive(true);
			
			WriteRenderTextureToDisk("snapshot", _renderTexture);
		}
		
		private void ExportRenderTargetImage(string filePath, IErcRenderable backgroundErcRenderable)
		{
			Debug.Log($"Loading Erc721Metadata JSON file [{filePath}]");
			
			var jsonData = File.ReadAllText(filePath);
			
			var metadata = JsonConvert.DeserializeObject<Erc721MetaData>(jsonData);
			
			ExportRenderTargetFromMetadata(metadata, backgroundErcRenderable);
		}
		
		private void WriteRenderTextureToDisk(string filename, RenderTexture renderTexture)
		{
			_canvasRootGameObject.transform.localScale = new Vector3(renderTexture.width / _referenceResolution.x, renderTexture.height / _referenceResolution.y);
			_camera.targetTexture = renderTexture;

			RenderTexture.active = renderTexture;

			_camera.Render();

			Texture2D image = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height);
			image.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0);
			image.Apply();
			RenderTexture.active = null;
			_camera.targetTexture = null;
			
			byte[] bytes = image.EncodeToPNG();
			DestroyImmediate(image);
			
			var path = Path.Combine(_exportFolderPath,filename + ".png");
			File.WriteAllBytes(path, bytes);
			
			Debug.Log($"[ Exported capture image {path} ]");
		}
		
		/// <summary>
		///  Load Equipment NFT attributes JSON data from disk
		/// </summary>
		private void LoadEquipmentAttributesData(string filePath)
		{
			if (!File.Exists(filePath)) throw new Exception($"File not found {filePath}");
			var jsonData = File.ReadAllText(filePath);
			_nftEquipmentAttributes = JsonConvert.DeserializeObject<NftEquipmentAttributes>(jsonData);
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
		
		/// <summary>
		/// Query string hash for a given string input given hash algorithm 
		/// </summary>
		private string GetHash(HashAlgorithm hashAlgorithm, string input)
		{
			byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
			
			var sBuilder = new StringBuilder();
			
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}
			
			return sBuilder.ToString();
		}
	}
}