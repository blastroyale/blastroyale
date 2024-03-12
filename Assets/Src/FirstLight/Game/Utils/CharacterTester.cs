#if UNITY_EDITOR
using FirstLight.Game.MonoComponent.Collections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class CharacterTester : MonoBehaviour
	{
		[Button]
		private void InstantiateCharacters()
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

		[Button]
		private void GiveWeapons(GameObject weapon)
		{
			var skins = gameObject.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.WeaponAnchor.localScale = Vector3.one;

				while (skin.WeaponAnchor.childCount > 0)
				{
					DestroyImmediate(skin.WeaponAnchor.GetChild(0).gameObject);
				}

				var weaponInstance = (GameObject) PrefabUtility.InstantiatePrefab(weapon, skin.WeaponAnchor);

				var weaponChild = weaponInstance.transform.GetChild(0);
				weaponChild.localPosition = Vector3.zero;
				weaponChild.localRotation = Quaternion.identity;
			}
		}

		[Button]
		private void GiveGlider(GameObject glider)
		{
			var skins = gameObject.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.GliderAnchor.localScale = Vector3.one;

				while (skin.GliderAnchor.childCount > 0)
				{
					DestroyImmediate(skin.GliderAnchor.GetChild(0).gameObject);
				}

				var weaponInstance = (GameObject) PrefabUtility.InstantiatePrefab(glider, skin.GliderAnchor);

				var weaponChild = weaponInstance.transform.GetChild(0);
				weaponChild.localPosition = Vector3.zero;
				weaponChild.localRotation = Quaternion.identity;
			}
		}
	}
}
#endif