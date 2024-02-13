using FirstLight.Game.Configs;
using FirstLight.Game.Messages;
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
		
		protected override void Awake()
		{
			base.Awake();
		
			_nextFlareTime = Random.Range(_animationConfig.FlareAnimMinPlaybackTime / 2, 
				_animationConfig.FlareAnimMaxPlaybackTime / 2);
			
			
			_services.MessageBrokerService.Subscribe<EquipmentScreenOpenedMessage>(OnEquipmentScreenOpenedMessage);
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpenedMessage);
		
		}

		private void Update()
		{
			if (!_processFlareAnimation)
			{
				return;
			}
			
			if (_currentIdleTime > _nextFlareTime)
			{
				_skin.TriggerFlair();
				_nextFlareTime = Random.Range(_animationConfig.FlareAnimMinPlaybackTime, _animationConfig.FlareAnimMaxPlaybackTime);
				
				_currentIdleTime = 0;
			}

			_currentIdleTime += Time.deltaTime;
		}

		public void PlayAnimation()
		{
			// Animator.SetTrigger(_triggerNamesClicked[Random.Range(0, _triggerNamesClicked.Length)]);
			var config = _animationConfig.AnimationNames;
			//_animator.SetTrigger(config[Random.Range(0, config.Length)]);
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