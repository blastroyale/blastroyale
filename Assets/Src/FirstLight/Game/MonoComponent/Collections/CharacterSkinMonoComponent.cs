using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.MonoComponent.Collections
{
	[RequireComponent(typeof(Animator))]
	public class CharacterSkinMonoComponent : MonoBehaviour
	{
		private static readonly Dictionary<AnchorType, GameIdGroup> AnchorMapping = new ()
		{
			{AnchorType.Weapon, GameIdGroup.Weapon},
			{AnchorType.Glider, GameIdGroup.Glider},
		};

		[InfoBox("If not set will use default animation!"), SerializeField]
		private RuntimeAnimatorController _menuController;

		[InfoBox("If not set will use default animation!"), SerializeField]
		private RuntimeAnimatorController _inGameController;

		[SerializeField] private List<Pair<AnchorType, List<Transform>>> _anchors;

		public RuntimeAnimatorController InGameController => _inGameController;

		public RuntimeAnimatorController MenuController => _menuController;


		private ReadOnlyDictionary<GameIdGroup, Transform[]> _anchorDictionary = new (new Dictionary<GameIdGroup, Transform[]>());

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
		[Button, InfoBox("Remove all anchors from GameObject and add new ones based on default anchors config!")]
		public void CreateAnchorsAutomatically()
		{
			DeleteAllAnchors();
			AddAnchorsAndFillRefs();
		}


		public void AddAnchorsAndFillRefs()
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
					foreach (var anchorConfig in entry.Value)
					{
						if (!AddObjectInside($"Anchor_{type}_{i}",anchorConfig.AttachToBone,out var obj))
						{
							UnityEditor.EditorUtility.DisplayDialog("Error", $"Could not find bone {anchorConfig.AttachToBone} to attach {type} anchor!","Cancel");
							_anchors.Clear();
							return;
						}
						obj.transform.localPosition = anchorConfig.Offset.Position;
						obj.transform.localRotation = Quaternion.Euler(anchorConfig.Offset.Rotation);
						var added = false;
						foreach (var anchor in _anchors.Where(anchor => anchor.Key == type))
						{
							anchor.Value.Add(obj.transform);
							added = true;
						}

						if (!added)
						{
							_anchors.Add(new Pair<AnchorType, List<Transform>>(type, new List<Transform>() {obj.transform}));
						}

						i++;
					}
				}
			};
		}

		private bool AddObjectInside(string objectName, string parentName, out GameObject createdObject )
		{
			var transform = GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == parentName);
			if (transform != null)
			{
				var obj = new GameObject(objectName);
				obj.transform.parent = transform;
				createdObject = obj;
				return true;
			}

			createdObject = null;
			return false;
		}

		public void DeleteAllAnchors()
		{
			foreach (var child in GetComponentsInChildren<Transform>())
			{
				var name = child.name;
				if (!name.Contains("Anchor_")) continue;
				DestroyImmediate(child.gameObject);
			}

			_anchors = new List<Pair<AnchorType, List<Transform>>>();
		}
#endif
	}
}