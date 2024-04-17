using System.Collections.Generic;
using FirstLight.Game.MonoComponent.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Path = System.IO.Path;

namespace FirstLight.Editor.Collections
{
	public class CollectionsWindow : OdinMenuEditorWindow
	{
		private const string PATH_CHARACTERS = "Assets/AddressableResources/Collections/CharacterSkins";

		[MenuItem("FLG/Collections/Open Menu")]
		private static void OpenWindow()
		{
			GetWindow<CollectionsWindow>("Collections").Show();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree();

			// Characters
			tree.Add("Characters", new CharactersGroup());
			var characterPrefabs = AssetDatabase.FindAssets("t:Model t:Prefab Char_", new[] {PATH_CHARACTERS});
			foreach (var guid in characterPrefabs)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var iconPath = path.Replace(".prefab", ".png").Replace(".fbx", ".png").Replace("Char_", "Icon_Char_");
				var characterName = Path.GetFileName(path).Replace("Char_", "").Replace(".prefab", "").Replace(".fbx", "");
				var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
				Debug.Log($"IconPath({characterName}): {iconPath}");

				tree.Add($"Characters/{characterName}", new CharacterSkinWrapper(path, icon), icon);
			}

			tree.Selection.SelectionChanged += OnSelectionChanged;

			return tree;
		}


		private void OnSelectionChanged(SelectionChangedType selectionChangedType)
		{
			if (MenuTree.Selection.SelectedValue is ICollectionWrapper selectedItem)
			{
				selectedItem.Load();
			}
		}

		private class CharacterSkinWrapper : ICollectionWrapper
		{
			private string PrefabPath { get; set; }

			[TitleGroup("Model")]
			[HorizontalGroup("Model/Split")]
			[VerticalGroup("Model/Split/Left")]
			[BoxGroup("Model/Split/Left/Info")]
			[InfoBox("These values are defined by the files / file names and cannot be changed here!")]
			[ShowInInspector, EnableGUI, InlineButton("@UnityEngine.GUIUtility.systemCopyBuffer = ID", "Copy")]
			private string ID { get; set; }

			[BoxGroup("Model/Split/Left/Assets")]
			[ShowInInspector]
			[DisableIf("@true")]
			private GameObject Prefab { get; set; }

			[ShowInInspector]
			[PreviewField(ObjectFieldAlignment.Left)]
			[BoxGroup("Model/Split/Left/Assets")]
			[DisableIf("@true")]
			private Sprite Icon { get; set; }

			[ShowInInspector]
			[PreviewField(ObjectFieldAlignment.Left)]
			[BoxGroup("Model/Split/Left/Assets")]
			[DisableIf("@true")]
			private Texture2D Texture { get; set; }

			[BoxGroup("Model/Split/Preview")]
			[InlineEditor(InlineEditorModes.LargePreview, PreviewHeight = 254, ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
			[ShowInInspector]
			private GameObject Preview => Prefab;

			[TitleGroup("Animations")]
			[ShowInInspector, HideLabel]
			private string ComingSoon => "Coming soon...";

			public CharacterSkinMonoComponent Skin { get; private set; }


			public CharacterSkinWrapper(string prefabPath, Sprite icon)
			{
				Icon = icon;
				PrefabPath = prefabPath;
			}

			public void Load()
			{
				ID = Path.GetFileNameWithoutExtension(PrefabPath);
				Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
				Skin = Prefab.GetComponent<CharacterSkinMonoComponent>();
				Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PrefabPath.Replace("Char_", "T_Char_").Replace(".fbx", ".png").Replace(".prefab", ".png"));
			}
		}

		private class CharactersGroup
		{
			[ShowInInspector]
			[TitleGroup("Characters")]
			[BoxGroup("Characters/Split/Addressable Paths")]
			[HorizontalGroup("Characters/Split")]
			[HideLabel]
			[ListDrawerSettings(DefaultExpandedState = true, IsReadOnly = true, NumberOfItemsPerPage = 5)]
			private List<string> AddressablesGroups { get; } = AddressablesUtility.GetAssetPathsInGroup("Collections_Characters");

			[Button, BoxGroup("Characters/Split/Tools")]
			private void OpenCharacterTestingScene()
			{
				EditorSceneManager.OpenScene("Assets/Art/SceneTests/AnimTest.unity");
			}
		}

		private interface ICollectionWrapper
		{
			void Load();
		}
	}
}