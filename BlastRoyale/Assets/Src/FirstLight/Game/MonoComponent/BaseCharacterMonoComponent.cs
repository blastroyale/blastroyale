using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.MonoComponent.MainMenu;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This Mono component controls loading and creation of player character equipment items and skin.
	/// </summary>
	public class BaseCharacterMonoComponent : MonoBehaviour
	{
		protected bool IsLoaded = false;

		[SerializeField, Required] protected UnityEvent _characterLoadedEvent;
		[SerializeField, Required] protected Transform _characterAnchor;
		public Vector3 Anchor => _characterAnchor.position;
		protected MainMenuCharacterViewComponent _characterViewComponent;
		protected IGameServices _services;
		protected CharacterSkinMonoComponent _skin;

		public MainMenuCharacterViewComponent CharacterViewComponent => _characterViewComponent;

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		public async UniTask UpdateSkin(ItemData skinId, bool playEnter)
		{
			if (CharacterViewComponent != null && CharacterViewComponent.gameObject != null)
			{
				Destroy(CharacterViewComponent.gameObject);
			}


			var obj = await _services.CollectionService.LoadCollectionItem3DModel(skinId, true, true);
			var container = obj.AddComponent<RenderersContainerMonoComponent>();
			container.UpdateRenderers();
			obj.AddComponent<RenderersContainerProxyMonoComponent>();
			var mm = obj.AddComponent<MainMenuCharacterViewComponent>();
			mm.PlayEnterAnimation = playEnter;
			AddDragCollider(obj);
			SkinLoaded(skinId, obj);
		}

		public UniTask UpdateMeleeSkin(ItemData skinId)
		{
			/*
			 Currently meta animations don't set the melee anchor position, so it doesn't work, waiting for animation changes
			 CharacterViewComponent.Cosmetics = new[] {skinId.Id};
			await CharacterViewComponent.InstantiateMelee();
			CharacterViewComponent.EquipWeapon(GameId.Random);*/
			return UniTask.CompletedTask;
		}

		/// <summary>
		/// Collider used for IDragHandler so we can rotate character on main menu
		/// </summary>
		/// <param name="obj"></param>
		private void AddDragCollider(GameObject obj)
		{
			// Legacy collider for old visibility volumes
			var newCollider = obj.AddComponent<CapsuleCollider>();
			newCollider.center = new Vector3(0, 0.75f, 0);
			newCollider.radius = 0.36f;
			newCollider.height = 1.5f;
			newCollider.direction = 1; // Y axis
			newCollider.isTrigger = false;
		}


		public void AnimateVictory()
		{
			if (_skin == null) return;
			_skin.TriggerVictory();
		}

		private void SkinLoaded(ItemData skin, GameObject instance)
		{
			instance.SetActive(false);

			var cacheTransform = instance.transform;

			cacheTransform.SetParent(_characterAnchor);

			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;

			_characterViewComponent = instance.GetComponent<MainMenuCharacterViewComponent>();

			cacheTransform.localScale = Vector3.one;

			instance.SetActive(true);

			_skin = instance.GetComponent<CharacterSkinMonoComponent>();
			IsLoaded = true;
			_characterLoadedEvent?.Invoke();
		}
	}
}