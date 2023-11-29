using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
		 private MainMenuCharacterAnimationConfig  _animationConfig => _services.ConfigsProvider.GetConfig<MainMenuCharacterAnimationConfig>();

		private float _currentIdleTime;
		private float _nextFlareTime = -1f;
		private bool _processFlareAnimation = true;
		private bool _playedFirstFlareAnim;
		private readonly int _flairHash = Animator.StringToHash("flair");
		
		protected override void Awake()
		{
			base.Awake();
		
			_nextFlareTime = Random.Range(_animationConfig.FlareAnimMinPlaybackTime / 2, 
				_animationConfig.FlareAnimMaxPlaybackTime / 2);
			
			
			_services.MessageBrokerService.Subscribe<EquipmentScreenOpenedMessage>(OnEquipmentScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpenedMessage);
		
		}

		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async UniTask Init(List<EquipmentInfo> items)
		{
			var list = new List<UniTask>();
			
			foreach (var item in items)
			{
				// TODO mihak: Make this into a single Equip method call
				list.Add(item.Equipment.IsWeapon() ? EquipWeapon(item.Equipment.GameId) : EquipItem(item.Equipment.GameId));
			}

			await UniTask.WhenAll(list);
		}
		
		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async UniTask Init(List<Equipment> items)
		{
			var list = new List<UniTask>();
			
			foreach (var item in items)
			{
				// TODO mihak: Make this into a single Equip method call
				list.Add(item.IsWeapon() ? EquipWeapon(item.GameId) : EquipItem(item.GameId));
			}

			await UniTask.WhenAll(list);
		}

		private void Update()
		{
			if (!_processFlareAnimation)
			{
				return;
			}
			
			if (_currentIdleTime > _nextFlareTime)
			{
				_animator.SetTrigger(_flairHash);
				_nextFlareTime = Random.Range(_animationConfig.FlareAnimMinPlaybackTime, _animationConfig.FlareAnimMaxPlaybackTime);
				
				_currentIdleTime = 0;
			}

			_currentIdleTime += Time.deltaTime;
		}

		public void PlayAnimation()
		{
			// Animator.SetTrigger(_triggerNamesClicked[Random.Range(0, _triggerNamesClicked.Length)]);
			var config = _animationConfig.AnimationNames;
			_animator.SetTrigger(config[Random.Range(0, config.Length)]);
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