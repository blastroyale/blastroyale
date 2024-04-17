using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using SRF;
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

		private GameObject _weaponGun;
		private GameObject _weaponMelee;
		protected IGameServices _services;

		private GameId[] _cosmetics = { };
		private WeaponType _equippedGunType;

		public GameId[] Cosmetics
		{
			get => _cosmetics;
			set => _cosmetics = value;
		}

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
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
			piece.localRotation = Quaternion.identity;
			piece.localScale = Vector3.one;
		}

		public async UniTask<GameObject> InstantiateMelee()
		{
			var anchor = _skin.WeaponMeleeAnchor; // TODO: Support XL melee

			var skinId = _services.CollectionService.GetCosmeticForGroup(_cosmetics, GameIdGroup.MeleeSkin);
			var weapon = await _services.CollectionService.LoadCollectionItem3DModel(skinId);
			var weaponTransform = weapon.transform;

			weaponTransform.SetParent(anchor);
			weaponTransform.ResetLocal();

			return weapon;
		}

		public async UniTask<GameObject> InstantiateWeapon(Equipment equip)
		{
			var weapon = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(equip.GameId);
			weapon.name = equip.GameId.ToString();

			if (this.IsDestroyed())
			{
				Destroy(weapon);
				return null;
			}

			if (_weaponGun != null)
			{
				if (weapon.TryGetComponent(out RenderersContainerMonoComponent renderersContainer))
				{
					_renderersContainerProxy.RemoveRenderersContainer(renderersContainer);
				}
				else if (weapon.transform.GetChild(0).TryGetComponent(out RenderersContainerMonoComponent c))
				{
					_renderersContainerProxy.RemoveRenderersContainer(c);
				}
				else
				{
					FLog.Error($"Unable to remove missing RenderersContainerMonoComponent {weapon.FullGameObjectPath()}");
				}

				Destroy(weapon);
			}

			_services.MessageBrokerService.Publish(new ItemEquippedMessage()
			{
				Character = gameObject.GetComponent<PlayerCharacterViewMonoComponent>(),
				Item = weapon,
				Id = equip.GameId
			});

			var config = _services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) equip.GameId);
			_equippedGunType = config.WeaponType;

			var weaponTransform = weapon.transform;
			var anchor = _skin.WeaponAnchor;

			weaponTransform.SetParent(anchor);
			weaponTransform.ResetLocal();
			weaponTransform.GetChild(0).ResetLocal();

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
				FLog.Error($"Unable to find RenderersContainerMonoComponent for {equip.GameId}");
			}

			_weaponGun = weapon;

			_services.MessageBrokerService.Publish(new EquipmentInstantiatedMessage()
			{
				Equipment = equip,
				Object = weapon
			});

			return weapon;
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
		/// Hide all Equipment currently equipped on a character.
		/// </summary>
		public void HideAllEquipment()
		{
			if (_weaponGun != null) _weaponGun.SetActive(false);
			if (_weaponMelee != null) _weaponMelee.SetActive(false);
		}

		/// <summary>
		/// Show all Equipment currently equipped on a character.
		/// </summary>
		public void ShowAllEquipment()
		{
			if (_weaponGun != null) _weaponGun.SetActive(true);
			if (_weaponMelee != null) _weaponMelee.SetActive(true);
		}

		/// <summary>
		/// Equip a weapon using a GameId
		/// </summary>
		public void EquipWeapon(Equipment equip)
		{
			if (equip.GameId == GameId.Hammer)
			{
				_skin.WeaponType = WeaponType.Melee;
				_skin.TriggerEquipMelee();
			}
			else
			{
				_skin.WeaponType = _equippedGunType;
				_skin.TriggerEquipGun();
			}
		}

		private void AddEquipmentRenderersContainer(RenderersContainerMonoComponent renderersContainer)
		{
			renderersContainer.SetLayer(gameObject.layer);
			renderersContainer.SetEnabled(_renderersContainerProxy.Enabled);

			_renderersContainerProxy.AddRenderersContainer(renderersContainer);
			Color col = default;
			if (_renderersContainerProxy.GetFirstRendererColor(ref col))
			{
				renderersContainer.SetColor(col);
			}
		}
	}
}