using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		[FormerlySerializedAs("_animator")] [SerializeField] protected Animator Animator;
		
		[SerializeField] private Transform[] _weaponAnchors;
		[SerializeField] private Transform[] _helmetAnchors;
		[SerializeField] private Transform[] _bootsAnchors;
		[SerializeField] private Transform[] _shieldAnchors;
		[SerializeField] private Transform[] _amuletAnchors;
		[SerializeField] private Transform[] _armorAnchors;
		[SerializeField, Required] private RenderersContainerProxyMonoComponent _renderersContainerProxy;
		
		private IDictionary<GameIdGroup, IList<GameObject>> _equipment;
		private IGameServices _services;
		
		private void OnValidate()
		{
			Animator = Animator ? Animator : GetComponent<Animator>();
			_renderersContainerProxy = _renderersContainerProxy ? _renderersContainerProxy : GetComponent<RenderersContainerProxyMonoComponent>();
			
			OnEditorValidate();
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_equipment = new Dictionary<GameIdGroup, IList<GameObject>>();
		}

		/// <summary>
		/// Equip characters equipment slot with an asset loaded by unique id.
		/// </summary>
		public async Task<List<GameObject>> EquipItem(GameId gameId)
		{
			var slot = gameId.GetSlot();
			
			var anchors = GetEquipmentAnchors(slot);
			var instances = new List<GameObject>(anchors.Length);
			var instance = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(gameId);
			
			if (this.IsDestroyed())
			{
				Destroy(instance);
				return instances;
			}
			
			if (_equipment.ContainsKey(slot))
			{
				UnequipItem(slot);
			}
			
			var childCount = instance.transform.childCount;
			
			for(var i = 0; i < Mathf.Max(childCount, 1); i++)
			{
				var piece = childCount > 0 ? instance.transform.GetChild(0) : instance.transform;
				
				piece.SetParent(anchors[i]);
				instances.Add(piece.gameObject);
				
				piece.localPosition = Vector3.zero;
				piece.localRotation = Quaternion.identity;
				
				if (piece.TryGetComponent<RenderersContainerMonoComponent>(out var renderContainer))
				{
					_renderersContainerProxy.AddRenderersContainer(renderContainer);
				}
			}

			if (childCount > 0)
			{
				Destroy(instance);
			}
			
			_equipment.Add(slot, instances);

			return instances;
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
				DestroyImmediate(items[i]);
			}

			_equipment.Remove(slotType);
		}

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

		protected virtual void OnEditorValidate() {}
		
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
				default:
					throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null);
			}
		}
	}
}