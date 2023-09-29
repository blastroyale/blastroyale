using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using Quantum;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace FirstLight.Game.MonoComponent
{
	[RequireComponent(typeof(Animator))]
	public class CharacterSkinMonoComponent : MonoBehaviour
	{
		private static readonly Dictionary<AnchorType, GameIdGroup> AnchorMapping = new()
		{
			{AnchorType.Amulet, GameIdGroup.Amulet},
			{AnchorType.Weapon, GameIdGroup.Weapon},
			{AnchorType.Helmet, GameIdGroup.Helmet},
			{AnchorType.Shield, GameIdGroup.Shield},
			{AnchorType.Armor, GameIdGroup.Armor},
			{AnchorType.Glider, GameIdGroup.Glider},
		};

		private static readonly Dictionary<AnchorType, List<string>> AnchorNaming = new()
		{
			{AnchorType.Amulet, new() {"Amulet"}},
			{AnchorType.Weapon, new() {"Weapon"}},
			{AnchorType.Helmet, new() {"Helmet", "Hat"}},
			{AnchorType.Shield, new() {"Shield"}},
			{AnchorType.Armor, new() {"Armor"}},
			{AnchorType.Glider, new() {"Glider"}},
		};

		[InfoBox("If not set will use default animation!"), SerializeField]
		private RuntimeAnimatorController _menuController;

		[InfoBox("If not set will use default animation!"), SerializeField]
		private RuntimeAnimatorController _inGameController;

		[SerializeField] private List<Pair<AnchorType, List<Transform>>> _anchors;

		public RuntimeAnimatorController InGameController => _inGameController;

		public RuntimeAnimatorController MenuController => _menuController;


		private ReadOnlyDictionary<GameIdGroup, Transform[]> _anchorDictionary = new(new Dictionary<GameIdGroup, Transform[]>());

		private void Awake()
		{
			var dictionary = new Dictionary<GameIdGroup, Transform[]>();

			foreach (var config in _anchors)
			{
				dictionary.Add(AnchorMapping[config.Key], config.Value.ToArray());
			}

			_anchorDictionary = new ReadOnlyDictionary<GameIdGroup, Transform[]>(dictionary);
		}


		public Transform[] GetEquipmentAnchors(GameIdGroup slotType)
		{
			if (_anchorDictionary.TryGetValue(slotType, out var value))
			{
				return value;
			}

			throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null);
		}


#if UNITY_EDITOR
		[Button,InfoBox("Remove all anchors from GameObject and add new ones based on default anchors config!")]
		public void CreateAnchorsAutomatically()
		{
			DeleteAllAnchors();
			AddAnchors();
		}

        
		public void AddAnchors()
		{
			var id = AddressableConfigLookup.GetConfig(AddressableId.Collections_CharacterSkins_Config);
			var op = Addressables.LoadAssetAsync<CharacterSkinConfigs>(id.Address);
			op.Completed += handle =>
			{
				var skinConfig = handle.Result;

				var anchors = skinConfig.Config.Anchors;

				foreach (var entry in anchors)
				{
					var type = entry.Key;
					var i = 1;
					foreach (var anchor in entry.Value)
					{
						var obj = new GameObject($"Anchor_{type}_{i}");
						AddObjectInside(anchor.AttachToBone, obj);
						obj.transform.localPosition = anchor.Offset.Position;
						obj.transform.localRotation = Quaternion.Euler(anchor.Offset.Rotation);
						i++;
					}
				}
			};
		}

		private void AddObjectInside(string name, GameObject obj)
		{
			var transform = GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
			if (transform != null)
			{
				obj.transform.parent = transform;
			}
		}
		
		public void DeleteAllAnchors()
		{
			foreach (var child in GetComponentsInChildren<Transform>())
			{
				var name = child.name;
				if (!name.Contains("Anchor")) continue;
				DestroyImmediate(child.gameObject);
			}
		}
        
		[Button,InfoBox("Searches for anchor references inside the object and add it to the anchors references")]
		public void FillSkinAnchorReferences()
		{
			_anchors = new List<Pair<AnchorType, List<Transform>>>();
			foreach (var child in GetComponentsInChildren<Transform>())
			{
				var name = child.name;
				if (!name.Contains("Anchor")) continue;
				var contains = AnchorNaming.Any(entry => entry.Value.Any(validName => name.Contains(validName)));
				if (!contains) continue;
				var key = AnchorNaming.First(entry => entry.Value.Any(validName => name.Contains(validName))).Key;

				var added = false;
				foreach (var anchor in _anchors.Where(anchor => anchor.Key == key))
				{
					anchor.Value.Add(child);
					added = true;
				}

				if (added)
				{
					continue;
				}

				_anchors.Add(new Pair<AnchorType, List<Transform>>(key, new List<Transform>() {child}));
			}
		}
#endif
	}
}