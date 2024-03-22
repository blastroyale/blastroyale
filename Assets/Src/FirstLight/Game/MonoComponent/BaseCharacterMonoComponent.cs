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

		protected MainMenuCharacterViewComponent _characterViewComponent;
		protected IGameServices _services;
		protected CharacterSkinMonoComponent _skin;
		protected List<Equipment> _equipment = new List<Equipment>();

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		public async UniTask UpdateSkin(ItemData skin, List<EquipmentInfo> equipment = null)
		{
			var equipmentList = equipment?.Select(equipmentInfo => equipmentInfo.Equipment).ToList();

			await UpdateSkin(skin, equipmentList);
		}

		public async UniTask UpdateSkin(ItemData skinId, List<Equipment> equipment = null)
		{
			if (_characterViewComponent != null && _characterViewComponent.gameObject != null)
			{
				Destroy(_characterViewComponent.gameObject);
			}

			_equipment = equipment;

			var obj = await _services.CollectionService.LoadCollectionItem3DModel(skinId, true,true);
			var container = obj.AddComponent<RenderersContainerMonoComponent>();
			container.UpdateRenderers();
			obj.AddComponent<RenderersContainerProxyMonoComponent>();
			obj.AddComponent<MainMenuCharacterViewComponent>();
			AddDragCollider(obj);
			SkinLoaded(skinId, obj);
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

		//TODO: use animator state instead
		public void RandomizeAnimationStateFrame(string animationStateName, int layer, float startNormalisedRange, float endNormalisedRange)
		{
			_skin.RandomizeAnimationStateFrame(animationStateName, layer, startNormalisedRange, endNormalisedRange);
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
