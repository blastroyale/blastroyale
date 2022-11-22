using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This Mono component controls loading and creation of player character equipment items and skin.
	/// </summary>
	public class MenuCharacterView : MonoBehaviour
	{
		private readonly int _equipRightHandHash = Animator.StringToHash("equip_hand_r");
		private readonly int _equipBodyHash = Animator.StringToHash("equip_body");
		private readonly int _victoryHash = Animator.StringToHash("victory");

		[SerializeField, Required] private UnityEvent _characterLoadedEvent;
		[SerializeField, Required] private Transform _characterAnchor;
		[SerializeField] private GameObject _testModel;
		
		private MainMenuCharacterViewComponent _characterViewComponent;
		private IGameServices _services;
		private Animator _animator;
		private List<Equipment> _equipment;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			if (_testModel != null)
			{
				Destroy(_testModel);
			}
		}

		public async Task UpdateSkin(GameId skin, List<Equipment> equipment)
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

		private async void EquipDefault()
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

			await _characterViewComponent.Init(_equipment);
			
			cacheTransform.localScale = Vector3.one;

			instance.SetActive(true);

			_animator = instance.GetComponent<Animator>();
			
			_characterLoadedEvent?.Invoke();
		}
	}
}