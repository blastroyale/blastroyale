using DebugUI;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
#endif

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class DebugCharacters : DebugUIBuilderBase
	{
#if UNITY_EDITOR
		public readonly string ALL_VALUE = "ALL";
		private static Dictionary<string, MethodInfo> _triggers;

		public Transform CharacterAnchor;
		public Transform Cameras;

		private string _currentCharacter;
		public MethodInfo _currentTrigger;
		private Transform _currentCamera;
		private GameObject _currentWeapon;
		private int _characterRotation;
		private WeaponType _weaponType = WeaponType.Melee;
		private bool _moving;
		private bool _aiming;

		public void Trigger()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				_currentTrigger.Invoke(skin, new object[] { });
			}
		}

		public void UpdateAnimationState()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.Moving = _moving;
				skin.Aiming = _aiming;
				skin.WeaponType = _weaponType;
			}
		}

		public void UpdateCurrentWeapon()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.WeaponAnchor.localScale = Vector3.one;

				while (skin.WeaponAnchor.childCount > 0)
				{
					DestroyImmediate(skin.WeaponAnchor.GetChild(0).gameObject);
				}

				if (!_currentWeapon) continue;
				var weaponInstance = (GameObject) PrefabUtility.InstantiatePrefab(_currentWeapon, skin.WeaponAnchor);
				weaponInstance.SetLayer(skin.gameObject.layer);
				// TODO: Temporary if, new weapons don't have a child
				if (weaponInstance.transform.childCount > 0)
				{
					var weaponChild = weaponInstance.transform.GetChild(0);
					weaponChild.localPosition = Vector3.zero;
					weaponChild.localRotation = Quaternion.identity;
				}
			}
		}

		private static Dictionary<string, MethodInfo> GetTriggers()
		{
			if (_triggers == null)
			{
				_triggers = typeof(CharacterSkinMonoComponent).GetMethods()
					.Where(m => m.Name.StartsWith("Trigger") && m.GetParameters().Length == 0)
					.ToDictionary(m => m.Name, m => m);
			}

			return _triggers;
		}

		public void ActivateCamera(Transform transform)
		{
			for (var i = 0; i < Cameras.childCount; i++)
			{
				var c = Cameras.GetChild(i);
				c.gameObject.SetActive(transform == c);
			}
		}

		public void UpdateCharacterRotation()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.transform.localRotation = Quaternion.Euler(0, _characterRotation, 0);
			}
		}

		public void LoadSkin(string id)
		{
			if (id == ALL_VALUE)
			{
				LoadAll();
				return;
			}

			DestroyAllSkins();

			var prfb = PrefabUtility.InstantiatePrefab(
				AssetDatabase.LoadAssetAtPath<CharacterSkinMonoComponent>(AssetDatabase.GUIDToAssetPath(_currentCharacter)), CharacterAnchor);

			UpdateAnimationState();
			UpdateCurrentWeapon();
			UpdateCharacterRotation();
		}

		public void DestroyAllSkins()
		{
			while (CharacterAnchor.childCount > 0)
			{
				DestroyImmediate(CharacterAnchor.GetChild(0).gameObject);
			}
		}

		private Dictionary<string, GameObject> GetAllWeapons()
		{
			var weapons = AssetDatabase.FindAssets("t:Prefab Weapon_", new[] {"Assets/AddressableResources/Weapons"});

			return weapons.Select(AssetDatabase.GUIDToAssetPath)
				.ToDictionary(path => Path.GetFileNameWithoutExtension(path).Replace("Weapon_", ""),
					path => AssetDatabase.LoadAssetAtPath<GameObject>(path));
		}

		[Button("Load all skins")]
		public void LoadAll()
		{
			DestroyAllSkins();

			const float SPACING = 2f;
			const int ROWS = 6;
			var indexZ = 0;
			var indexX = 0;
			var assets = AssetDatabase.FindAssets("t:Prefab Char_", new[] {"Assets/AddressableResources/Collections/CharacterSkins"});
			foreach (var character in assets)
			{
				var prfb = PrefabUtility.InstantiatePrefab(
					AssetDatabase.LoadAssetAtPath<CharacterSkinMonoComponent>(AssetDatabase.GUIDToAssetPath(character)), CharacterAnchor);

				var instance = (CharacterSkinMonoComponent) prfb;

				instance.transform.position = new Vector3(-indexX * SPACING, 0, indexZ * SPACING);

				if (indexZ == ROWS - 1)
				{
					indexX++;
				}

				indexZ = (indexZ + 1) % ROWS;
			}

			UpdateAnimationState();
			UpdateCurrentWeapon();
			UpdateCharacterRotation();
		}

		// Start is called before the first frame update
		protected override void Configure(IDebugUIBuilder builder)
		{
			_characterRotation = 90;
			_currentTrigger = GetTriggers().First().Value;

			builder.ConfigureWindowOptions(options =>
			{
				options.Title = "Debug Character";
				options.Draggable = true;
				options.Expanded = true;
			});

			var chars = AssetDatabase.FindAssets("t:Prefab Char_", new[] {"Assets/AddressableResources/Collections/CharacterSkins"})
				.ToDictionary(id => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(id)).Replace("Char_", ""), id => id);
			_currentCharacter = chars.First().Value;

			chars.Add("All", ALL_VALUE);
			builder.AddField("Skin", chars, (id) =>
			{
				_currentCharacter = id;
				LoadSkin(id);
			}, () => _currentCharacter);

			builder.AddField("Trigger", GetTriggers(), (id) => _currentTrigger = id, () => _currentTrigger);
			builder.AddButton("Play", Trigger);

			var cameras = new Dictionary<string, Transform>();
			for (int i = 0; i < Cameras.childCount; i++)
			{
				cameras[Cameras.GetChild(i).name] = Cameras.GetChild(i);
			}

			builder.AddField("Moving", () => _moving, (v) =>
			{
				_moving = v;
				UpdateAnimationState();
			});
			builder.AddField("Aiming", () => _aiming, (v) =>
			{
				_aiming = v;
				UpdateAnimationState();
			});
			var weaponDict = GetAllWeapons();
			weaponDict["None"] = null;
			builder.AddField("Weapon", weaponDict, (v) =>
			{
				_currentWeapon = v;
				UpdateCurrentWeapon();
			}, () => _currentWeapon);
			builder.AddField("Weapon Type", () => _weaponType, (v) =>
			{
				_weaponType = v;
				UpdateAnimationState();
			});
			builder.AddField("Camera", cameras, (c) =>
			{
				_currentCamera = c;
				ActivateCamera(c);
			}, () => _currentCamera);
			builder.AddSlider("Character Rotation", 0, 360, () => _characterRotation, (v) =>
			{
				_characterRotation = v;
				UpdateCharacterRotation();
			});
			UpdateCharacterRotation();
			ActivateCamera(cameras.First().Value);
			UpdateAnimationState();
			UpdateCurrentWeapon();
		}

		protected override void Awake()
		{
			base.Awake();
			LoadSkin(_currentCharacter);
		}
#else
	protected override void Configure(IDebugUIBuilder builder){}
#endif
	}
}