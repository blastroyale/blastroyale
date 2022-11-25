using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FirstLight.Game.Configs;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Utils;
using I2.Loc;
using Newtonsoft.Json;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Equipment = Quantum.Equipment;


namespace FirstLight.Editor.EditorTools.NFTGenerator
{
	/// <summary>
	/// This editor window provides functionality for generating render texture exported images for GameIds
	/// based on NFT metadata 
	/// </summary>
	public class NFTGeneratorImageEditorWindow : OdinEditorWindow
	{
		private enum RenderTextureMode
		{
			Standard,
			Standalone,
			Both
		}

		private enum TextureMode
		{
			Png
		}

		[MenuItem("FLG/NFT Generator ImageEditorWindow")]
		private static void OpenWindow()
		{
			GetWindow<NFTGeneratorImageEditorWindow>("NFT Generator ImageEditorWindow").Show();
		}

		[BoxGroup("Folder Paths")] [FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _exportFolderPath;

		[BoxGroup("Folder Paths")] [FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _importFolderPath;

		public string _metadataJsonFilePath;

		[SerializeField] private RenderTexture _renderTexture;
		[SerializeField] private RenderTexture _renderTextureStandalone;
		[SerializeField] private int _subFolderId;
		[SerializeField] private int _collectionId = 1;
		[SerializeField] private BaseEquipmentStatConfigs baseEquipmentStatConfigs;
		[SerializeField] private Vector2 _referenceResolution = new(1600, 2048);
		[SerializeField] private string _webMarketplaceUri = "https://flgmarketplacestorage.z33.web.core.windows.net";
		
		[SerializeField] private RenderTextureMode _renderTextureMode = RenderTextureMode.Both;
		[SerializeField] private TextureMode _textureMode = TextureMode.Png;
		
		private readonly Dictionary<GameId, GameObject> _assetDictionary = new Dictionary<GameId, GameObject>();
		private GameObject _canvas;
		private Transform _markerTransform;
		private GameObject _canvasRoot;
		private Camera _camera;
		
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

			InitializeReferences();

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

			await Addressables.LoadAssetsAsync<GameObject>(keys as IEnumerable, addressable =>
			{
				if (Enum.TryParse(addressable.name, out GameId gameId))
				{
					var go = Instantiate(addressable, _markerTransform);
					go.SetActive(false);
					_assetDictionary.Add(gameId, go);
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

			DestroyPool();

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

			InitializeReferences();
			
			_assetDictionary.Clear();

			var backgroundErcRenderable = _canvas.GetComponent<IErcRenderable>();

			var metadata = JsonConvert.DeserializeObject<Erc721MetaData>(File.ReadAllText(_metadataJsonFilePath));

			Debug.Log($"Loading Erc721Metadata JSON file [{_metadataJsonFilePath}]");

			QualitySettings.SetQualityLevel(2, true);

			var gameId = (GameId) metadata.attibutesDictionary["subCategory"];

			var asset = await Addressables
			                  .LoadAssetAsync<GameObject>($"AdventureAssets/items/{gameId.ToString()}.prefab").Task;
			
			
			var go = Instantiate(asset, _markerTransform);
			_assetDictionary.Add(gameId, go);
			
			ExportRenderTextureFromMetadata(metadata, backgroundErcRenderable);

			_assetDictionary.Clear();
			
			DestroyPool();
		}

		private void InitializeReferences()
		{
			_camera = GameObject.Find("Camera").GetComponent<Camera>();
			_markerTransform = GameObject.Find("Marker").transform;
			_canvas = GameObject.Find("Canvas");
			_canvasRoot = _canvas.transform.GetChild(0).gameObject;
		}

		private void DestroyPool()
		{
			var childCount = _markerTransform.transform.childCount;
			for (var i = childCount - 1; i >= 0; i--)
			{
				GameObject.DestroyImmediate(_markerTransform.transform.GetChild(i).gameObject);
			}
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

			InitializeReferences();

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

			var layer = UnityEngine.LayerMask.NameToLayer("Default");
				
			var operationHandle = await Addressables.LoadAssetsAsync<GameObject>(keys as IEnumerable, addressable =>
			{
				if (Enum.TryParse(addressable.name, out GameId gameId))
				{
					var go = Instantiate(addressable, _markerTransform);
					go.SetLayer(layer, true);
					go.SetActive(false);
					_assetDictionary.Add(gameId, go);
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

					for (var rarityIndex = 0; rarityIndex < (int) EquipmentRarity.TOTAL; rarityIndex++)
					{
						for (var materialIndex = 0; materialIndex < (int) EquipmentMaterial.TOTAL; materialIndex++)
						{
							for (var factionIndex = 0; factionIndex < (int) EquipmentFaction.TOTAL; factionIndex++)
							{
								for (var gradeIndex = 0; gradeIndex < (int) EquipmentGrade.TOTAL; gradeIndex++)
								{
									for (var adjectiveIndex = 0;
									     adjectiveIndex < (int) EquipmentAdjective.TOTAL;
									     adjectiveIndex++)
									{
										metadata.name =
											LocalizationManager.GetTranslation($"GameIds/{gameId.ToString()}");
										metadata.attibutesDictionary["category"] = (int) gameIdGroups.ElementAt(categoryIndex);
										metadata.attibutesDictionary["subCategory"] = (int) gameId;

										var config = baseEquipmentStatConfigs.Configs.First(c => c.Id == gameId);
										metadata.attibutesDictionary["manufacturer"] = (int) config.Manufacturer;

										metadata.attibutesDictionary["rarity"] = 0;
										metadata.attibutesDictionary["material"] = 0;
										metadata.attibutesDictionary["faction"] = 0;
										metadata.attibutesDictionary["grade"] = 0;
										metadata.attibutesDictionary["adjective"] = 0;

										var hash = GenerateImageFilenameHash(metadata);
										metadata.image =
											$"{_webMarketplaceUri}/nftimages/{_subFolderId}/{_collectionId}/{hash}.png";

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

			DestroyPool();

			Debug.Log($"Exported [{imagesExportedCount}] image combinations");
		}
		

		/// <summary>
		/// Export a render texture image given a metadata object and renderable background
		/// </summary>
		private void ExportRenderTextureFromMetadata(Erc721MetaData metadata, IErcRenderable backgroundErcRenderable)
		{
			var subcategoryId = metadata.attibutesDictionary["subCategory"];
			var gameId = (GameId) subcategoryId;

			var go = _assetDictionary[gameId];
			go.SetActive(true);
			go.transform.SetParent(_markerTransform);
			go.transform.localScale = Vector3.one;
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.Euler(-7.215f, -53.91f, -8.94f);

			var ercRenderable = go.GetComponent<IErcRenderable>();
			ercRenderable?.Initialise(new Equipment()
			{
				Faction = (EquipmentFaction) metadata.attibutesDictionary["faction"],
				Material = (EquipmentMaterial) metadata.attibutesDictionary["material"],
				Adjective = (EquipmentAdjective) metadata.attibutesDictionary["adjective"],
				Rarity = (EquipmentRarity) metadata.attibutesDictionary["rarity"],
			});
			
			
			var bounds = GetBounds(go); 
			go.transform.position = -bounds.center;
			
			var size = bounds.size;
			var max = Mathf.Max(size.x, size.y, size.z);
			var frustumHeight = max  / _camera.aspect;

			var margin = 1.3f;
			var distance = (frustumHeight * margin) * 0.5f / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			_camera.transform.position = new Vector3(-distance, 0, 0);
			
			_canvas.SetActive(true);

			backgroundErcRenderable?.Initialise(new Equipment()
			{
				GameId = gameId,
				Faction = (EquipmentFaction) metadata.attibutesDictionary["faction"],
				Material = (EquipmentMaterial) metadata.attibutesDictionary["material"],
				Adjective = (EquipmentAdjective) metadata.attibutesDictionary["adjective"],
				Rarity = (EquipmentRarity) metadata.attibutesDictionary["rarity"],
				Grade = (EquipmentGrade)metadata.attibutesDictionary["grade"],
			});

			if (_renderTextureMode == RenderTextureMode.Standard || _renderTextureMode == RenderTextureMode.Both)
			{
				WriteRenderTextureToDisk(Path.GetFileNameWithoutExtension(metadata.image), _renderTexture);
			}

			_canvas.SetActive(false);

			if (_renderTextureMode == RenderTextureMode.Standalone || _renderTextureMode == RenderTextureMode.Both)
			{
				WriteRenderTextureToDisk($"{Path.GetFileNameWithoutExtension(metadata.image)}_standalone", _renderTextureStandalone, true);
			}

			go.SetActive(false);
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
		private void WriteRenderTextureToDisk(string filename, RenderTexture renderTexture, bool crop = false)
		{
			var ext = _textureMode == TextureMode.Png ? ".png" : ".jpg";

			var path = Path.Combine(_exportFolderPath, filename + ext);
			
			_canvasRoot.transform.localScale = new Vector3(renderTexture.width / _referenceResolution.x,
			                                               renderTexture.height / _referenceResolution.y, 1);
			_camera.targetTexture = renderTexture;

			RenderTexture.active = renderTexture;

			_camera.Render();

			var width = _camera.targetTexture.width;
			var height = _camera.targetTexture.height;

			Texture2D image = new Texture2D(width, height);
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
				

				var copyWidth = Math.Abs(maxCroppedWidth - minCroppedWidth);
				var copyHeight = Math.Abs(maxCroppedHeight - minCroppedHeight);

				Texture2D copyTexture = new Texture2D(copyWidth, copyHeight);
				copyTexture.ReadPixels(new Rect(minCroppedWidth, minCroppedHeight, copyWidth, copyHeight), 0, 0);
				copyTexture.Apply();

				RenderTexture.active = null;
				_camera.targetTexture = null;

				byte[] bytes = _textureMode == TextureMode.Png ? copyTexture.EncodeToPNG() : copyTexture.EncodeToJPG();

				DestroyImmediate(copyTexture);
				DestroyImmediate(image);
				
				File.WriteAllBytes(path, bytes);
			}
			else
			{
				RenderTexture.active = null;
				_camera.targetTexture = null;

				byte[] bytes = _textureMode == TextureMode.Png ? image.EncodeToPNG() : image.EncodeToJPG();
				
				DestroyImmediate(image);
				
				File.WriteAllBytes(path, bytes);
			}
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

					if (renderer.enabled && renderer.gameObject.activeSelf)
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

					if (renderer.enabled && renderer.gameObject.activeSelf)
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



