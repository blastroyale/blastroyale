using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This Mono component is used to lookup the inventory anchor transforms for a given
	/// character by slot type and query of a slot has an item equipped.
	/// </summary>
	public abstract class CharacterEquipmentMonoComponent : MonoBehaviour
	{
		private RenderersContainerProxyMonoComponent _renderersContainerProxy;
		protected CharacterSkinMonoComponent _skin;

		private IDictionary<GameIdGroup, IList<GameObject>> _equipment;
		protected IGameServices _services;

		private GameId[] _cosmetics = { };

		public GameId[] Cosmetics
		{
			get => _cosmetics;
			set => _cosmetics = value;
		}

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_equipment = new Dictionary<GameIdGroup, IList<GameObject>>();
			_skin = GetComponent<CharacterSkinMonoComponent>();
			_renderersContainerProxy = GetComponent<RenderersContainerProxyMonoComponent>();
		}

		/// <summary>
		/// Instantiate a Game Item of the specified GameIdGroup
		/// </summary>
		public async UniTask InstantiateGlider(ItemData item)
		{
			var anchor = _skin.GliderAnchor;
			var instance = await _services.CollectionService.LoadCollectionItem3DModel(item);

			if (this.IsDestroyed())
			{
				Destroy(instance);

				return;
			}

			var piece = instance.transform;
			piece.SetParent(anchor);

			piece.localPosition = Vector3.zero;
			piece.localRotation = Quaternion.Euler(0, 90, 0); // TODO mihak: Temp hack
			piece.localScale = Vector3.one;
		}

		protected async UniTask<GameObject> InstantiateWeapon(Equipment equip)
		{
			// TODO Generic GameId to GameIDGroup skin converter
			var gameId = equip.GameId;
			GameObject obj;
			if (gameId == GameId.Hammer)
			{
				var skinId = _services.CollectionService.GetCosmeticForGroup(_cosmetics, GameIdGroup.MeleeSkin);
				obj = await _services.CollectionService.LoadCollectionItem3DModel(skinId, false, true);
			}
			else
			{
				obj = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(gameId);
			}

			obj.name = gameId.ToString();
			return obj;
		}

		/// <summary>
		/// Equip characters equipment slot with an asset loaded by unique id.
		/// </summary>
		private async UniTask<GameObject> EquipWeaponInternal(Equipment equip)
		{
			var gameId = equip.GameId;
			var slot = gameId.GetSlot();


			var instance = await InstantiateWeapon(equip);

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return instance;
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

			var config = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) equip.GameId);
			_skin.WeaponType = config.WeaponType;

			var weaponTransform = instance.transform;
			var anchor = _skin.WeaponAnchor;

			weaponTransform.SetParent(anchor);

			weaponTransform.localPosition = new Vector3(0, 0.1f, 0); // TODO mihak: TEMP HACK
			weaponTransform.localRotation = Quaternion.Euler(0, 115, 0); // TODO mihak: TEMP HACK
			weaponTransform.localScale = Vector3.one;

			if (weaponTransform.TryGetComponent<RenderersContainerMonoComponent>(out var renderContainer))
			{
				AddEquipmentRenderersContainer(renderContainer);
			}
			else if (weaponTransform.GetChild(0).TryGetComponent<RenderersContainerMonoComponent>(out var c))
			{
				AddEquipmentRenderersContainer(c);
			}
			else
			{
				FLog.Error($"Unable to find RenderersContainerMonoComponent for {gameId}");
			}

			_equipment.Add(slot, new[] {instance}); // TODO: Ugly temporary thing
			_services.MessageBrokerService.Publish(new EquipmentInstantiatedMessage()
			{
				Equipment = equip,
				Object = instance
			});
			return instance;
		}
		
		/// <summary>
		/// Destroy an item currently equipped on the character.
		/// </summary>
		public void DestroyGlider()
		{
			var anchor = _skin.GliderAnchor;

			if (anchor.childCount != 0)
			{
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
		public async UniTask<GameObject> EquipWeapon(Equipment equip)
		{
			var weapon = await EquipWeaponInternal(equip);

			// We set the first child to 0 pos because that's the actual weapon and that offset is
			// there for the spawners as they use the same prefab.
			weapon.transform.GetChild(0).localPosition = Vector3.zero;

			return weapon;
		}

		private void AddEquipmentRenderersContainer(RenderersContainerMonoComponent renderersContainer)
		{
			renderersContainer.SetLayer(gameObject.layer);
			_renderersContainerProxy.AddRenderersContainer(renderersContainer);
			Color col = default;
			if (_renderersContainerProxy.GetFirstRendererColor(ref col))
			{
				renderersContainer.SetColor(col);
			}
		}
	}
}