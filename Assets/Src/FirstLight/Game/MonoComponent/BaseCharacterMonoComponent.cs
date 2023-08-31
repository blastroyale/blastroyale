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
		private readonly int _victoryHash = Animator.StringToHash("victory");
		
		[SerializeField, Required] protected UnityEvent _characterLoadedEvent;
		[SerializeField, Required] protected Transform _characterAnchor;
		
		protected MainMenuCharacterViewComponent _characterViewComponent;
		protected IGameServices _services;
		protected Animator _animator;
		protected List<Equipment> _equipment = new List<Equipment>();

		protected virtual void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
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
			
			cacheTransform.localScale = Vector3.one;

			instance.SetActive(true);

			_animator = instance.GetComponent<Animator>();
			IsLoaded = true;
			_characterLoadedEvent?.Invoke();
		}
	}
}