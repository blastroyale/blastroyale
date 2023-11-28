using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FirstLight.Game.Configs;
using FirstLight.Game.UIElements;
using I2.Loc;
using JetBrains.Annotations;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.EditorTools.NFTGenerator
{
	/// <summary>
	/// Generates equipment cards for the marketplace.
	/// </summary>
	public class EquipmentCardGeneratorWindow : OdinEditorWindow
	{
		[SerializeField, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		private string _outputDirectory;

		[SerializeField] private TextureMode _textureMode;
		[SerializeField] private string _webMarketplaceUri = "https://flgmarketplacestorage.z33.web.core.windows.net";
		[SerializeField] private int _subFolderId;
		[SerializeField] private int _collectionId = 1;

		private UIDocument _document;
		private EquipmentCardElement _card;
		private RenderTexture _rt;
		private BaseEquipmentStatConfigs _statConfigs;

		private IEnumerator<Equipment> _equipmentList;
		private int _processedImages;
#pragma warning disable CS0414
		[UsedImplicitly] private bool _processing;
#pragma warning restore CS0414

		[MenuItem("FLG/Generators/Equipment Card Generator")]
		private static void OpenWindow()
		{
			GetWindow<EquipmentCardGeneratorWindow>("Equipment Card Generator").Show();
		}

		protected override void Initialize()
		{
			base.Initialize();
			_statConfigs = LoadStatConfigs();
		}

		[Button, HideInPlayMode]
		private void PrepareEnvironment()
		{
			Debug.Log("ECG | Preparing environment...");

			EditorSceneManager.OpenScene("Assets/Scenes/Editor/EquipmentCardRender.unity");
			EditorApplication.isPlaying = true;

			Debug.Log("ECG | Done.");
		}

		[Button, ShowIf("@IsEnvironmentReady() && !_processing")]
		private void StartProcessing()
		{
			Debug.Log("ECG | Starting processing...");

			_document = FindObjectOfType<UIDocument>();
			_card = _document.rootVisualElement.Q<EquipmentCardElement>("Card");
			_rt = _document.panelSettings.targetTexture;
			_equipmentList = AllEquipment();
			_processedImages = 0;
			_processing = true;

			EditorApplication.update += ProcessItems;
		}

		[Button, ShowIf("@_processing")]
		private void StopProcessing()
		{
			Debug.Log("ECG | Processing done.");
			EditorApplication.update -= ProcessItems;
			EditorApplication.isPlaying = false;

			_processedImages = 0;
			_processing = false;
		}

		private void Generate(Equipment e)
		{
			var metadata = new Erc721MetaData
			{
				name = LocalizationManager.GetTranslation($"GameIds/{e.GameId.ToString()}"),
				attibutesDictionary = new Dictionary<string, int>
				{
					["category"] = (int) e.GetEquipmentGroup(),
					["subCategory"] = (int) e.GameId,
					["manufacturer"] = (int) _statConfigs.Configs.First(c => c.Id == e.GameId).Manufacturer,
					["rarity"] = (int) e.Rarity,
					["material"] = (int) e.Material,
					["faction"] = (int) e.Faction,
					["grade"] = (int) e.Grade,
					["adjective"] = (int) e.Adjective
				}
			};
			var hash = GenerateImageFilenameHash(metadata);

			metadata.image = $"{_webMarketplaceUri}/nftimages/{_subFolderId}/{_collectionId}/{hash}.png";

			WriteRenderTextureToDisk(Path.GetFileNameWithoutExtension(metadata.image));
		}

		private void ProcessItems()
		{
			if (_card.UniqueId.IsValid)
			{
				Generate(_equipmentList.Current);
				_processedImages++;
				Debug.Log($"ECG | Processing: {_processedImages}/a lot");
			}

			if (_equipmentList.MoveNext())
			{
				_card.SetEquipment(_equipmentList.Current , loadEditorSprite: true);
			}
			else
			{
				StopProcessing();
			}
		}

		[UsedImplicitly]
		private bool IsEnvironmentReady()
		{
			return Application.isPlaying && SceneManager.GetActiveScene().name == "EquipmentCardRender";
		}

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

			using var sha256Hash = SHA256.Create();
			var hash = GetHash(sha256Hash, stringBuilder.ToString());
			return hash;
		}

		private string GetHash(HashAlgorithm hashAlgorithm, string input)
		{
			var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

			var sb = new StringBuilder();

			foreach (var b in data)
			{
				sb.Append(b.ToString("x2"));
			}

			return sb.ToString();
		}

		private IEnumerator<Equipment> TestEquipment()
		{
			yield return new Equipment(GameId.ApoShotgun, faction: EquipmentFaction.Chaos);
			yield return new Equipment(GameId.ApoShotgun, faction: EquipmentFaction.Celestial);
			yield return new Equipment(GameId.ApoShotgun, faction: EquipmentFaction.Organic);
		}

		private IEnumerator<Equipment> AllEquipment()
		{
			var groups = new[]
			{
				GameIdGroup.Helmet,
				GameIdGroup.Shield,
				GameIdGroup.Armor,
				GameIdGroup.Amulet,
				GameIdGroup.Weapon
			};

			foreach (var group in groups)
			{
				foreach (var id in group.GetIds())
				{
					if (id == GameId.Hammer) continue;

					for (var ri = 0; ri < (int) EquipmentRarity.TOTAL; ri++)
					{
						for (var mi = 0; mi < (int) EquipmentMaterial.TOTAL; mi++)
						{
							for (var fi = 0; fi < (int) EquipmentFaction.TOTAL; fi++)
							{
								for (var gi = 0; gi < (int) EquipmentGrade.TOTAL; gi++)
								{
									for (var ai = 0; ai < (int) EquipmentAdjective.TOTAL; ai++)
									{
										yield return new Equipment(
											id,
											rarity: (EquipmentRarity) ri,
											material: (EquipmentMaterial) mi,
											faction: (EquipmentFaction) fi,
											grade: (EquipmentGrade) gi,
											adjective: (EquipmentAdjective) ai
										);
									}
								}
							}
						}
					}
				}
			}
		}

		private BaseEquipmentStatConfigs LoadStatConfigs()
		{
			var assets = AssetDatabase.FindAssets("t:BaseEquipmentStatConfigs");
			Assert.IsTrue(assets.Length == 1, "More than 1 BaseEquipmentStatConfigs found!");
			return AssetDatabase.LoadAssetAtPath<BaseEquipmentStatConfigs>(AssetDatabase.GUIDToAssetPath(assets[0]));
		}

		public void WriteRenderTextureToDisk(string filename, bool crop = false)
		{
			var ext = _textureMode == TextureMode.Png ? ".png" : ".jpg";
			var path = Path.Combine(_outputDirectory, filename + ext);

			RenderTexture.active = _rt;

			var width = _rt.width;
			var height = _rt.height;

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

				var bytes = _textureMode == TextureMode.Png ? copyTexture.EncodeToPNG() : copyTexture.EncodeToJPG();

				DestroyImmediate(copyTexture);
				DestroyImmediate(image);

				File.WriteAllBytes(path, bytes);
			}
			else
			{
				var bytes = _textureMode == TextureMode.Png ? image.EncodeToPNG() : image.EncodeToJPG();

				DestroyImmediate(image);

				File.WriteAllBytes(path, bytes);
			}
		}

		private enum TextureMode
		{
			Png,
			Jpg
		}
	}
}