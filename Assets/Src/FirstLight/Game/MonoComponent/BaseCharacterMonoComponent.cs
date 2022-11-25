using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
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
		protected readonly int _equipRightHandHash = Animator.StringToHash("equip_hand_r");
		protected readonly int _equipBodyHash = Animator.StringToHash("equip_body");
		protected readonly int _victoryHash = Animator.StringToHash("victory");

		[SerializeField, Required] protected UnityEvent _characterLoadedEvent;
		[SerializeField, Required] protected Transform _characterAnchor;
		[SerializeField] protected GameObject _testModel;
		
		protected MainMenuCharacterViewComponent _characterViewComponent;
		protected IGameServices _services;
		protected Animator _animator;
		
		private List<Equipment> _equipment;

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			if (_testModel != null)
			{
				Destroy(_testModel);
			}
		}

		public async Task UpdateSkin(GameId skin, List<EquipmentInfo> equipment = null)
		{
			var equipmentList = equipment?.Select(equipmentInfo => equipmentInfo.Equipment).ToList();

			await UpdateSkin(skin, equipmentList);
		}
		
		public async Task UpdateSkin(GameId skin, List<Equipment> equipment = null)
		{
			if (_characterViewComponent != null && _characterViewComponent.gameObject != null)
			{
				Destroy(_characterViewComponent.gameObject);
			}

			_equipment = equipment;
			
			var instance = await _services.AssetResolverService.RequestAsset<GameId, GameObject>(skin, true, true);

			await SkinLoaded(skin, instance);
		}

		public void AnimateVictory()
		{
			_animator.SetTrigger(_victoryHash);
		}

		protected async void EquipDefault()
		{
			await _characterViewComponent.EquipItem(GameId.Hammer);
		}

		private async Task SkinLoaded(GameId id, GameObject instance)
		{
			instance.SetActive(false);

			var cacheTransform = instance.transform;

			cacheTransform.SetParent(_characterAnchor);

			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;

			_characterViewComponent = instance.GetComponent<MainMenuCharacterViewComponent>();

			if (_equipment != null)
			{
				await _characterViewComponent.Init(_equipment);
			}
			
			if (_equipment == null || !_equipment.Exists(equipment => equipment.IsWeapon()))
			{
				EquipDefault();
			}

			cacheTransform.localScale = Vector3.one;

			instance.SetActive(true);

			_animator = instance.GetComponent<Animator>();
			
			_characterLoadedEvent?.Invoke();
		}
	}
}