using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.MonoComponent
{
	[RequireComponent(typeof(Animator))]
	public class CharacterSkinMonoComponent : MonoBehaviour
	{
		[InfoBox("If not set will use default animation!"), SerializeField]
		private RuntimeAnimatorController _menuController;

		[InfoBox("If not set will use default animation!"), SerializeField]
		private RuntimeAnimatorController _inGameController;

		[SerializeField] private List<Transform> _weaponAnchors;
		[SerializeField] private List<Transform> _helmetAnchors;
		[SerializeField] private List<Transform> _bootsAnchors;
		[SerializeField] private List<Transform> _shieldAnchors;
		[SerializeField] private List<Transform> _amuletAnchors;
		[SerializeField] private List<Transform> _armorAnchors;
		[SerializeField] private Transform _gliderAnchor;

		public RuntimeAnimatorController InGameController => _inGameController;

		public RuntimeAnimatorController MenuController => _menuController;

		public Transform[] GetEquipmentAnchors(GameIdGroup slotType)
		{
			switch (slotType)
			{
				case GameIdGroup.Weapon:
					return _weaponAnchors.ToArray();
				case GameIdGroup.Helmet:
					return _helmetAnchors.ToArray();
				case GameIdGroup.Shield:
					return _shieldAnchors.ToArray();
				case GameIdGroup.Amulet:
					return _amuletAnchors.ToArray();
				case GameIdGroup.Armor:
					return _armorAnchors.ToArray();
				case GameIdGroup.Glider:
					return new[] {_gliderAnchor};
				default:
					throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null);
			}
		}
		
		[Button]
		public void FillAnchorsAutomatically()
		{
			_weaponAnchors.Clear();
			_helmetAnchors.Clear();
			_bootsAnchors.Clear();
			_shieldAnchors.Clear();
			_amuletAnchors.Clear();
			_armorAnchors.Clear();
			_gliderAnchor = null;

			foreach (Transform child in GetComponentsInChildren<Transform>())
			{
				var name = child.name;
				Debug.Log(name);
				if (name.Contains("Anchor"))
				{
					if (name.Contains("_Weapon"))
					{
						_weaponAnchors.Add(child);
					}

					if (name.Contains("_Boot"))
					{
						_bootsAnchors.Add(child);
					}

					if (name.Contains("_Hat"))
					{
						_helmetAnchors.Add(child);
					}

					if (name.Contains("_Shield"))
					{
						_shieldAnchors.Add(child);
					}

					if (name.Contains("_Armor"))
					{
						_armorAnchors.Add(child);
					}

					if (name.Contains("_Amulet"))
					{
						_amuletAnchors.Add(child);
					}

					if (name.Contains("Glider"))
					{
						_gliderAnchor = child;
					}
				}
			}
		}
	}
}