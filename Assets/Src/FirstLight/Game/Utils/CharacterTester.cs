#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.MonoComponent.Collections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class CharacterTester : MonoBehaviour
	{
		[ShowInInspector, ValueDropdown("GetAllWeapons", FlattenTreeView = true), OnValueChanged("GiveWeapons")]
		private GameObject _weapon;

		[ShowInInspector, ValueDropdown("GetAllGliders", FlattenTreeView = true), OnValueChanged("GiveGliders")]
		private GameObject _glider;

		[Button]
		private void RefreshCharacters()
		{
			const float SPACING = 1.5f;
			const int ROWS = 3;

			var assets = AssetDatabase.FindAssets("t:Prefab Char_", new[] {"Assets/AddressableResources/Collections/CharacterSkins"});

			// Destroy kids
			while (transform.childCount > 0)
			{
				DestroyImmediate(transform.GetChild(0).gameObject);
			}

			var indexZ = 0;
			var indexX = 0;
			foreach (var character in assets)
			{
				var prfb = PrefabUtility.InstantiatePrefab(
					AssetDatabase.LoadAssetAtPath<CharacterSkinMonoComponent>(AssetDatabase.GUIDToAssetPath(character)), gameObject.transform);

				var instance = (CharacterSkinMonoComponent) prfb;

				instance.transform.position = new Vector3(-indexX * SPACING, 0, indexZ * SPACING);

				if (indexZ == ROWS - 1)
				{
					indexX++;
				}

				indexZ = (indexZ + 1) % ROWS;
			}
		}

		private IEnumerable<ValueDropdownItem<GameObject>> GetAllWeapons()
		{
			var weapons = AssetDatabase.FindAssets("t:Prefab Weapon_", new[] {"Assets/AddressableResources/Weapons"});

			return weapons.Select(AssetDatabase.GUIDToAssetPath)
				.Select(path => new ValueDropdownItem<GameObject>(Path.GetFileNameWithoutExtension(path).Replace("Weapon_", ""),
					AssetDatabase.LoadAssetAtPath<GameObject>(path)));
		}

		private IEnumerable<ValueDropdownItem<GameObject>> GetAllGliders()
		{
			var gliders = AssetDatabase.FindAssets("t:Prefab Glider_", new[] {"Assets/AddressableResources/Gliders"});

			return gliders.Select(AssetDatabase.GUIDToAssetPath)
				.Select(path => new ValueDropdownItem<GameObject>(Path.GetFileNameWithoutExtension(path).Replace("Glider_", ""),
					AssetDatabase.LoadAssetAtPath<GameObject>(path)));
		}

		private void GiveWeapons()
		{
			var skins = gameObject.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.WeaponAnchor.localScale = Vector3.one;

				while (skin.WeaponAnchor.childCount > 0)
				{
					DestroyImmediate(skin.WeaponAnchor.GetChild(0).gameObject);
				}

				var weaponInstance = (GameObject) PrefabUtility.InstantiatePrefab(_weapon, skin.WeaponAnchor);

				// TODO: Temporary if, new weapons don't have a child
				if (weaponInstance.transform.childCount > 0)
				{
					var weaponChild = weaponInstance.transform.GetChild(0);
					weaponChild.localPosition = Vector3.zero;
					weaponChild.localRotation = Quaternion.identity;
				}
			}
		}

		private void GiveGliders()
		{
			var skins = gameObject.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.GliderAnchor.localScale = Vector3.one;

				while (skin.GliderAnchor.childCount > 0)
				{
					DestroyImmediate(skin.GliderAnchor.GetChild(0).gameObject);
				}

				var weaponInstance = (GameObject) PrefabUtility.InstantiatePrefab(_glider, skin.GliderAnchor);

				var weaponChild = weaponInstance.transform.GetChild(0);
				weaponChild.localPosition = Vector3.zero;
				weaponChild.localRotation = Quaternion.identity;
			}
		}
	}
}
#endif