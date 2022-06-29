using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Utils;
using I2.Loc;
using Newtonsoft.Json;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using LayerMask = UnityEngine.LayerMask;


namespace Src.FirstLight.Tools
{
	[Serializable]
	public struct CategoryPrefabData
	{
		public GameObject[] GameObjects;
	}
	
	/// <summary>
	/// This Mono component provides functionality for generating render texture exported images for GameIds
	/// based on NFT metadata 
	/// </summary>
	public class ImageCaptureService : MonoBehaviour
	{
		private enum RenderTextureMode
		{
			Standard,
			Standalone,
			Both
		}

		private enum TextureMode
		{
			Png,
			Jpg
		}

		[BoxGroup("Folder Paths")]
		[FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _exportFolderPath;
		[BoxGroup("Folder Paths")]
		[FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _importFolderPath;
		[FilePath(Extensions = "json")]
		public string _metadataJsonFilePath;
		
		[SerializeField] private Transform _markerTransform;
		[SerializeField] private RenderTexture _renderTexture;
		[SerializeField] private RenderTexture _renderTextureStandalone;
		[SerializeField] private Camera _camera;
		[SerializeField] private GameObject _canvas;
		[SerializeField] private GameObject _canvasRoot;
		[SerializeField] private RenderTextureMode _renderTextureMode;
		[SerializeField] private TextureMode _textureMode;
		[SerializeField] private int _subFolderId;
		[SerializeField] private int _collectionId;
		[SerializeField] private BaseEquipmentStatsConfigs _baseEquipmentStatsConfigs;
		[SerializeField] private Vector2 _referenceResolution;
		[SerializeField] private string _webMarketplaceUri;
		
		private Dictionary<GameId, GameObject> _assetDictionary = new Dictionary<GameId, GameObject>();
		
		/// <summary>
		/// Export render texture image collection referencing NFT metadata json folder 
		/// </summary>
		[Button("Export Metadata Collection")]
		public async void ExportMetadataCollection()
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
			
			var gameIdGroups = new[]
			{
				GameIdGroup.Helmet, 
				GameIdGroup.Shield,
				GameIdGroup.Armor,
				GameIdGroup.Amulet,
				GameIdGroup.Weapon
			};
			
			
			_assetDictionary.Clear();
			var keys = new List<object>();
			
			for (var categoryIndex = 0; categoryIndex < gameIdGroups.Length; categoryIndex++)
			{
				var ids = gameIdGroups[categoryIndex].GetIds();
				for (var subCategoryIndex = 0; subCategoryIndex < ids.Count; subCategoryIndex++)
				{
					var gameId = ids.ElementAt(subCategoryIndex);
					keys.Add($"AdventureAssets/items/{gameId.ToString()}.prefab");
				}
			}
			
			var operationHandle = await Addressables.LoadAssetsAsync<GameObject>(keys, addressable =>
			{
				if (Enum.TryParse(addressable.name, out GameId gameId))
				{
					_assetDictionary.Add(gameId, addressable);
				}
				else
				{
					throw new Exception($"Unable to parse GameId {addressable.name}");
				}
			}, Addressables.MergeMode.Union, false).Task;
			
			var backgroundErcRenderable = _canvas.GetComponent<IErcRenderable>();

			var fileCount = 0;
			foreach (var filePath in Directory.EnumerateFiles(_importFolderPath, "*.json"))
			{
				var metadata = JsonConvert.DeserializeObject<Erc721MetaData>(File.ReadAllText(filePath));
				ExportRenderTextureFromMetadata(metadata, backgroundErcRenderable);
				fileCount++;
			}
			
			Debug.Log($"Loaded [{fileCount} metadata files]");
		}
		
		/// <summary>
		/// Export render texture image referencing NFT metadata json file 
		/// </summary>
		[Button("Export Render Texture [Metadata Json]")]
		public async void ExportRenderTextureFromMetadataJson()
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
			
			_assetDictionary.Clear();
			
			var backgroundErcRenderable = _canvas.GetComponent<IErcRenderable>();
			
			var metadata = JsonConvert.DeserializeObject<Erc721MetaData>(File.ReadAllText(_metadataJsonFilePath));
			
			Debug.Log($"Loading Erc721Metadata JSON file [{_metadataJsonFilePath}]");

			var gameId = (GameId)metadata.attibutesDictionary["subCategory"];
			
			var asset = await Addressables.LoadAssetAsync<GameObject>($"AdventureAssets/items/{gameId.ToString()}.prefab").Task;
				   
			_assetDictionary.Add(gameId, asset);
			
			ExportRenderTextureFromMetadata(metadata, backgroundErcRenderable);
		}
		
		/// <summary>
		/// Export all render texture images for Gameids 
		/// </summary>
		[Button("Export All Render Textures")]
		private async void ExportAllRenderTextures()
		{
			if (_exportFolderPath == "" || !Directory.Exists(_exportFolderPath))
			{
				Debug.LogError($"Invalid export folder path [{_exportFolderPath}]");
				
				return;
			}
			
			var gameIdGroups = new[]
			{
				GameIdGroup.Helmet, 
				GameIdGroup.Shield,
				GameIdGroup.Armor,
				GameIdGroup.Amulet,
				GameIdGroup.Weapon
			};
			
			
			_assetDictionary.Clear();
			var keys = new List<object>();
			
			for (var categoryIndex = 0; categoryIndex < gameIdGroups.Length; categoryIndex++)
			{
				var ids = gameIdGroups[categoryIndex].GetIds();
				for (var subCategoryIndex = 0; subCategoryIndex < ids.Count; subCategoryIndex++)
				{
					var gameId = ids.ElementAt(subCategoryIndex);
					keys.Add($"AdventureAssets/items/{gameId.ToString()}.prefab");
				}
			}
			
			var operationHandle = await Addressables.LoadAssetsAsync<GameObject>(keys, addressable =>
			{
				if (Enum.TryParse(addressable.name, out GameId gameId))
                {
                   _assetDictionary.Add(gameId, addressable);
                }
				else
				{
					throw new Exception($"Unable to parse GameId {addressable.name}");
				}
			}, Addressables.MergeMode.Union, false).Task; 
			
			
			var backgroundErcRenderable = _canvas.GetComponent<IErcRenderable>();

			var metadata = new Erc721MetaData {attibutesDictionary = new Dictionary<string, int>()};
			
			var imagesExportedCount = 0;
			
			for (var categoryIndex = 0; categoryIndex < gameIdGroups.Length; categoryIndex++)
			{
				var ids = gameIdGroups[categoryIndex].GetIds();
				
				for (var subCategoryIndex = 0; subCategoryIndex < ids.Count; subCategoryIndex++)
				{
					var gameId = ids.ElementAt(subCategoryIndex);

					if (gameId == GameId.Hammer)
					{
						continue;
					}

					for (var rarityIndex = 0; rarityIndex < (int)EquipmentRarity.TOTAL; rarityIndex++)
					{
						for (var materialIndex = 0; materialIndex < (int)EquipmentMaterial.TOTAL; materialIndex++)
						{
							for (var factionIndex = 0; factionIndex < (int)EquipmentFaction.TOTAL; factionIndex++)
							{
								for (var gradeIndex = 0; gradeIndex < (int)EquipmentGrade.TOTAL; gradeIndex++)
								{
									for (var adjectiveIndex = 0; adjectiveIndex < (int) EquipmentAdjective.TOTAL; adjectiveIndex++)
									{
										metadata.name = LocalizationManager.GetTranslation ($"GameIds/{gameId.ToString()}");
										metadata.attibutesDictionary["category"] = (int)gameIdGroups.ElementAt(categoryIndex);
										metadata.attibutesDictionary["subCategory"] = (int)gameId;
										
										var config =_baseEquipmentStatsConfigs.Configs.First(c => c.Id == gameId);
										metadata.attibutesDictionary["manufacturer"] = (int)config.Manufacturer;
										
										metadata.attibutesDictionary["rarity"] = rarityIndex;
										metadata.attibutesDictionary["material"] = materialIndex;
										metadata.attibutesDictionary["faction"] = factionIndex;
										metadata.attibutesDictionary["grade"] = gradeIndex;
										metadata.attibutesDictionary["adjective"] = adjectiveIndex;

										var hash = GenerateImageFilenameHash(metadata);
										metadata.image = $"{_webMarketplaceUri}/nftimages/{_subFolderId}/{_collectionId}/{hash}.png";

										ExportRenderTextureFromMetadata(metadata, backgroundErcRenderable);
										
										imagesExportedCount++;
									}
								}
							}
						}
					}
				}
			}

			for (var i = 0; i < operationHandle.Count; i++)
			{
				Addressables.Release(operationHandle.ElementAt(i));
			}

			Debug.Log($"Exported [{imagesExportedCount}] image combinations");
		}
		
		
		/// <summary>
		/// Export a render texture image given a metadata object and renderable background
		/// </summary>
		private void ExportRenderTextureFromMetadata(Erc721MetaData metadata, IErcRenderable backgroundErcRenderable)
		{
			if (_markerTransform.transform.childCount > 0)
			{
				DestroyImmediate(_markerTransform.transform.GetChild(0).gameObject);
			}
        			
			var subcategoryId = metadata.attibutesDictionary["subCategory"];
			var gameId = (GameId) subcategoryId;

			var asset = _assetDictionary[gameId];
			
			
			var go = Instantiate(asset);
			go.transform.SetParent(_markerTransform);
			go.transform.localScale = Vector3.one;
			go.transform.localPosition = Vector3.zero;
			
			go.SetLayer(LayerMask.NameToLayer("Default"), true);
			
			var ercRenderable = go.GetComponent<IErcRenderable>();
			ercRenderable?.Initialise(metadata);
			
			var bounds = GetBounds(go);
			go.transform.localPosition = -bounds.center;
			go.transform.localRotation = Quaternion.identity;

			var max = bounds.size;
			var radius = max.magnitude / 2f;
			var horizontalFOV =
				2f * Mathf.Atan(Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad / 2f) * _camera.aspect) *
				Mathf.Rad2Deg;
			var fov = Mathf.Min(_camera.fieldOfView, horizontalFOV);
			var dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));

			_camera.transform.position = new Vector3(-dist, 0, 0);
			
			
			_canvas.SetActive(true);
			
			backgroundErcRenderable?.Initialise(metadata);

			if (_renderTextureMode == RenderTextureMode.Standard || _renderTextureMode == RenderTextureMode.Both)
			{
				WriteRenderTextureToDisk(Path.GetFileNameWithoutExtension(metadata.image), _renderTexture);
			}

			_canvas.SetActive(false);

			if (_renderTextureMode == RenderTextureMode.Standalone || _renderTextureMode == RenderTextureMode.Both)
			{
				WriteRenderTextureToDisk($"{Path.GetFileNameWithoutExtension(metadata.image)}_standalone", _renderTextureStandalone);
			}
			
			_canvas.SetActive(true);
		}

		
		/// <summary>
		/// Generate image filename unique hash given metadata object
		/// </summary>
		private string GenerateImageFilenameHash(Erc721MetaData metadata)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(metadata.attibutesDictionary["category"]);
			stringBuilder.Append(metadata.attibutesDictionary["subCategory"]);
			stringBuilder.Append(metadata.attibutesDictionary["manufacturer"]);
			stringBuilder.Append(metadata.attibutesDictionary["rarity"]);
			stringBuilder.Append(metadata.attibutesDictionary["material"]);
			stringBuilder.Append(metadata.attibutesDictionary["faction"]);
			stringBuilder.Append(metadata.attibutesDictionary["grade"]);
			stringBuilder.Append(metadata.attibutesDictionary["adjective"]);
			
			using (var sha256Hash = SHA256.Create())
			{
				var hash = GetHash(sha256Hash, stringBuilder.ToString());
				return hash;
			}
		}

		/// <summary>
		///  Write render texture image to disk given a file path and render texture object
		/// </summary>
		private void WriteRenderTextureToDisk(string filename, RenderTexture renderTexture)
		{
			_canvasRoot.transform.localScale = new Vector3(renderTexture.width / _referenceResolution.x,
			                                               renderTexture.height / _referenceResolution.y, 1);
			_camera.targetTexture = renderTexture;

			RenderTexture.active = renderTexture;

			_camera.Render();

			Texture2D image = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height);
			image.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0);
			image.Apply();
			RenderTexture.active = null;
			_camera.targetTexture = null;
			
			byte[] bytes = _textureMode == TextureMode.Png ? image.EncodeToPNG() : image.EncodeToJPG();
			DestroyImmediate(image);

			var ext = _textureMode == TextureMode.Png ? ".png" : ".jpg";
			
			var path = Path.Combine(_exportFolderPath, filename + ext);
			File.WriteAllBytes(path, bytes);
		}
		
		
		/// <summary>
		/// Query bounding box for a given game object
		/// </summary>
		private Bounds GetBounds(GameObject go)
		{
			var bounds = new Bounds();

			var renderers = go.GetComponentsInChildren<Renderer>(true);
			
			if (renderers.Length > 0)
			{
				foreach (Renderer renderer in renderers)
				{
					if (renderer is ParticleSystemRenderer)
					{
						continue;
					}

					if (renderer.enabled && gameObject.activeSelf)
					{
						bounds = renderer.bounds;
						
						break;
					}
				}
				
				foreach (Renderer renderer in renderers)
				{
					if (renderer is ParticleSystemRenderer)
					{
						continue;
					}
					
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
			var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
			
			var sBuilder = new StringBuilder();
			
			for (var i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}
			
			return sBuilder.ToString();
		}
	}
}