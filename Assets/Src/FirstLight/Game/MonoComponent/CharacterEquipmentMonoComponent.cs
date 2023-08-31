using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This Mono component is used to lookup the inventory anchor transforms for a given
	/// character by slot type and query of a slot has an item equipped.
	/// </summary>
	public abstract class CharacterEquipmentMonoComponent : MonoBehaviour
	{
		[FormerlySerializedAs("_animator")] [SerializeField]
		protected Animator Animator;

		[SerializeField] private Transform[] _weaponAnchors;
		[SerializeField] private Transform[] _helmetAnchors;
		[SerializeField] private Transform[] _bootsAnchors;
		[SerializeField] private Transform[] _shieldAnchors;
		[SerializeField] private Transform[] _amuletAnchors;
		[SerializeField] private Transform[] _armorAnchors;
		[SerializeField] private Transform _gliderAnchor;
		[SerializeField, Required] private RenderersContainerProxyMonoComponent _renderersContainerProxy;

		private IDictionary<GameIdGroup, IList<GameObject>> _equipment;
		protected IGameServices _services;

		private void OnValidate()
		{
			Animator = Animator ? Animator : GetComponent<Animator>();
			_renderersContainerProxy = _renderersContainerProxy ? _renderersContainerProxy : GetComponent<RenderersContainerProxyMonoComponent>();

			OnEditorValidate();
		}

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_equipment = new Dictionary<GameIdGroup, IList<GameObject>>();
		}

		/// <summary>
		/// Instantiate a Game Item of the specified GameIdGroup
		/// </summary>
		public async Task<List<GameObject>> InstantiateItem(GameId gameId, GameIdGroup gameIdGroup)
		{
			var anchors = GetEquipmentAnchors(gameIdGroup);
			var instance = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(gameId);
			var instances = new List<GameObject>(anchors.Length);

			if (this.IsDestroyed())
			{
				Destroy(instance);

				return instances;
			}

			var piece = instance.transform;
			piece.SetParent(anchors[0]);

			piece.localPosition = Vector3.zero;
			piece.localRotation = Quaternion.identity;
			piece.localScale = Vector3.one;
			instances.Add(piece.gameObject);

			return instances;
		}

		protected virtual async Task<GameObject> InstantiateEquipment(GameId gameId)
		{
			var obj = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(gameId);
			obj.name = gameId.ToString();
			return obj;
		}

		/// <summary>
		/// Equip characters equipment slot with an asset loaded by unique id.
		/// </summary>
		public async Task<List<GameObject>> EquipItem(GameId gameId)
		{
			var slot = gameId.GetSlot();

			var anchors = GetEquipmentAnchors(slot);
			var instances = new List<GameObject>();
			var instance = await InstantiateEquipment(gameId);
			
			if (this.IsDestroyed())
			{
				Destroy(instance);
				return instances;
			}

			if (_equipment.ContainsKey(slot))
			{
				UnequipItem(slot);
			}
			
			_services.MessageBrokerService.Publish(new ItemEquippedMessage()
			{
				Character = gameObject.GetComponent<PlayerCharacterViewMonoComponent>(),
				Item = instance,
				Id = gameId
			});

			var childCount = instance.transform.childCount;

			// We detach the first child of the equipment and copy it to the anchor
			// Not sure why
			for (var i = 0; i < Mathf.Max(childCount, 1); i++)
			{
				var piece = childCount > 0 ? instance.transform.GetChild(0) : instance.transform;

				piece.SetParent(anchors[i]);
				instances.Add(piece.gameObject);

				piece.localPosition = Vector3.zero;
				piece.localRotation = Quaternion.identity;
				piece.localScale = Vector3.one;

				if (piece.TryGetComponent<RenderersContainerMonoComponent>(out var renderContainer))
				{
					renderContainer.SetLayer(gameObject.layer);
					_renderersContainerProxy.AddRenderersContainer(renderContainer);
				}
			}

			// If we detached the child of a parent, we destroy the parent
			if (childCount > 0)
			{
#if UNITY_EDITOR
				Log.Warn("Unecessary destroy of child of equipment hack triggered, please fix");
#endif
				Destroy(instance);
			}

			_equipment.Add(slot, instances);

			return instances;
		}

		/// <summary>
		/// Destroy an item currently equipped on the character.
		/// </summary>
		public void DestroyItem(GameIdGroup slotType)
		{
			var anchors = GetEquipmentAnchors(slotType);
			for (var i = 0; i < anchors.Length; i++)
			{
				if (i >= anchors.Length) continue;
				var anchor = anchors[i];
				if (anchor.childCount == 0) continue;
				for (var c = 0; c < anchor.childCount; c++)
				{
					var child = anchor.GetChild(c).gameObject;
					child.SetActive(false);
					Destroy(child);
				}
			}
		}

		/// <summary>
		/// UnEquip an equipment slot destroying any game object references.
		/// </summary>
		public void UnequipItem(GameIdGroup slotType)
		{
			if (!_equipment.ContainsKey(slotType))
			{
				Debug.LogWarning($"Cannot unequip item of type {slotType} - _equipment does not contain Key of this type");
				return;
			}

			var items = _equipment[slotType];

			for (var i = 0; i < items.Count; i++)
			{
				_renderersContainerProxy.RemoveRenderersContainer(items[i].GetComponent<RenderersContainerMonoComponent>());
				items[i].SetActive(false);
				Destroy(items[i]);
			}

			_equipment.Remove(slotType);
		}

		/// <summary>
		/// Hide all Equipment currently equipped on a character.
		/// </summary>
		public void HideAllEquipment()
		{
			foreach (var items in _equipment.Values)
			{
				for (var i = 0; i < items.Count; i++)
				{
					items[i].SetActive(false);
				}
			}
		}

		/// <summary>
		/// Show all Equipment currently equipped on a character.
		/// </summary>
		public void ShowAllEquipment()
		{
			foreach (var items in _equipment.Values)
			{
				for (var i = 0; i < items.Count; i++)
				{
					items[i].SetActive(true);
				}
			}
		}

		/// <summary>
		/// Equip a weapon using a GameId
		/// </summary>
		public async Task<IList<GameObject>> EquipWeapon(GameId weapon)
		{
			var weapons = await EquipItem(weapon);

			for (var i = 0; i < weapons.Count; i++)
			{
				Animator.runtimeAnimatorController = weapons[i].GetComponent<RuntimeAnimatorMonoComponent>().AnimatorController;
			}

			return weapons;
		}

		protected virtual void OnEditorValidate()
		{
		}

		private Transform[] GetEquipmentAnchors(GameIdGroup slotType)
		{
			switch (slotType)
			{
				case GameIdGroup.Weapon:
					return _weaponAnchors;
				case GameIdGroup.Helmet:
					return _helmetAnchors;
				case GameIdGroup.Shield:
					return _shieldAnchors;
				case GameIdGroup.Amulet:
					return _amuletAnchors;
				case GameIdGroup.Armor:
					return _armorAnchors;
				case GameIdGroup.Glider:
					return new[] { _gliderAnchor };
				default:
					throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null);
			}
		}
	}
}