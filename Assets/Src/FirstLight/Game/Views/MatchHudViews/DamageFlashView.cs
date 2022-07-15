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

		private EntityRef _entityFollowed;
		
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_services.MessageBrokerService.Subscribe<SpectateTargetSwitchedMessage>(OnSpectateTargetSwitchedMessage);
			
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnEventOnHealthChanged);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			_entityFollowed = callback.Entity;
		}

		private void OnSpectateTargetSwitchedMessage(SpectateTargetSwitchedMessage obj)
		{
			_entityFollowed = obj.EntitySpectated;
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || _entityFollowed != EntityRef.None)
			{
				return;
			}

			_entityFollowed = callback.Entity;
		}

		private void OnEventOnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != _entityFollowed || callback.CurrentHealth >= callback.PreviousHealth)
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
