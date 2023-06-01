using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <inheritdoc cref="CharacterEquipmentMonoComponent"/>
	public class MainMenuCharacterViewComponent : CharacterEquipmentMonoComponent, IDragHandler
	{
		[SerializeField] private MainMenuCharacterAnimationConfigs _mainMenuCharacterAnimations;

		private IGameDataProvider _gameDataProvider;
		private float _currentIdleTime;
		private float _nextFlareTime = -1f;
		private bool _processFlareAnimation = true;
		private bool _playedFirstFlareAnim;
		private readonly int _flairHash = Animator.StringToHash("flair");
		
		protected override void Awake()
		{
			base.Awake();
			
			_nextFlareTime = Random.Range(_mainMenuCharacterAnimations.Configs[0].FlareAnimMinPlaybackTime / 2, 
											_mainMenuCharacterAnimations.Configs[0].FlareAnimMaxPlaybackTime / 2);
			
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_services.MessageBrokerService.Subscribe<EquipmentScreenOpenedMessage>(OnEquipmentScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpenedMessage);
		}

		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async Task Init(List<EquipmentInfo> items)
		{
			var list = new List<Task>();
			
			foreach (var item in items)
			{
				// TODO mihak: Make this into a single Equip method call
				list.Add(item.Equipment.IsWeapon() ? EquipWeapon(item.Equipment.GameId) : EquipItem(item.Equipment.GameId));
			}

			await Task.WhenAll(list);
		}
		
		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async Task Init(List<Equipment> items)
		{
			var list = new List<Task>();
			
			foreach (var item in items)
			{
				// TODO mihak: Make this into a single Equip method call
				list.Add(item.IsWeapon() ? EquipWeapon(item.GameId) : EquipItem(item.GameId));
			}

			await Task.WhenAll(list);
		}

		private void Update()
		{
			if (!_processFlareAnimation)
			{
				return;
			}
			
			if (_currentIdleTime > _nextFlareTime)
			{
				Animator.SetTrigger(_flairHash);
				_nextFlareTime = Random.Range(_mainMenuCharacterAnimations.Configs[0].FlareAnimMinPlaybackTime, _mainMenuCharacterAnimations.Configs[0].FlareAnimMaxPlaybackTime);
				_currentIdleTime = 0;
			}

			_currentIdleTime += Time.deltaTime;
		}

		public void PlayAnimation()
		{
			// Animator.SetTrigger(_triggerNamesClicked[Random.Range(0, _triggerNamesClicked.Length)]);
			var config = _mainMenuCharacterAnimations.Configs[0].AnimationNames;
			Animator.SetTrigger(config[Random.Range(0, config.Length)]);
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			transform.parent.Rotate(0, -eventData.delta.x, 0, Space.Self);
		}

		private void OnEquipmentScreenOpenedMessage(EquipmentScreenOpenedMessage message)
		{
			_processFlareAnimation = false;
		}

		private void OnPlayScreenOpenedMessage(PlayScreenOpenedMessage message)
		{
			_processFlareAnimation = true;
		}
	}
}