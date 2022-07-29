using System;
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
		private IMatchServices _matchServices;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);

			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnEventOnHealthChanged);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			_entityFollowed = next.Entity;
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