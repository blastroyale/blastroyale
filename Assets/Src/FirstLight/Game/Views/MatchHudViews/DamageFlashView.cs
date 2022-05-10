using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Used to display the red flash that indicates to a player about damage received
	/// </summary>
	public class DamageFlashView : MonoBehaviour
	{
		[SerializeField, Required] private Animation _damageFlashAnimation;
		
		private EntityRef Entity { get; set; }
		
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_services.MessageBrokerService.Subscribe<HealthEntityInstantiatedMessage>(OnEntityInstantiated);
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnEventOnHealthChanged, onlyIfActiveAndEnabled : true);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnEntityInstantiated(HealthEntityInstantiatedMessage message)
		{
			var frame = message.Game.Frames.Verified;
			var entity = message.Entity.EntityRef;

			frame.TryGet<PlayerCharacter>(entity, out var playerCharacter);

			if (message.Game.PlayerIsLocal(playerCharacter.Player))
			{
				Entity = entity;
			}
		}
		
		private void OnEventOnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != Entity || callback.CurrentHealth >= callback.PreviousHealth)
			{
				return;
			}
			
			if (callback.CurrentHealth <= 0)
			{
				// THis code is like that because sometimes Rewind() doesn't put animation back to the first frame
				// https://forum.unity.com/threads/animation-rewind-not-working.4756/
				_damageFlashAnimation.clip.SampleAnimation(_damageFlashAnimation.gameObject, 0f);
				return;
			}
			
			_damageFlashAnimation.Rewind();
			_damageFlashAnimation.Play();
		}
	}
}
