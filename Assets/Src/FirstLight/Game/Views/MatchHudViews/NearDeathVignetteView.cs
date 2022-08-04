using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the vignette that indicates to a player about low health
	/// </summary>
	public class NearDeathVignetteView : MonoBehaviour
	{
		private const float NEAR_DEATH_VIGNETTE_HEALTH_RATIO_THRESHOLD = 0.4f;
		private const float MAX_ALPHA_HEALTH_RATIO_THRESHOLD = 0.15f;
		private const float STARTING_ALPHA = 0.25f;

		private const float ALPHA_CHANGE = (1f - STARTING_ALPHA) /
		                                   (NEAR_DEATH_VIGNETTE_HEALTH_RATIO_THRESHOLD -
		                                    MAX_ALPHA_HEALTH_RATIO_THRESHOLD);

		[SerializeField] private Image _vignetteImage;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private EntityRef _entityFollowed;

		private void Awake()
		{
			_vignetteImage.enabled = false;
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);

			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnEventOnHealthChanged);
			QuantumEvent.Subscribe<EventOnHealthIsZero>(this, OnEventOnHealthIsZero);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			_entityFollowed = next.Entity;

			var frame = QuantumRunner.Default.Game.Frames.Verified;
			
			if(frame.TryGet<Stats>(_entityFollowed, out var stats))
			{
				SetVignetteIntensity(stats.CurrentHealth, stats.Values[(int) StatType.Health].StatValue.AsInt);
			}
		}

		private void OnEventOnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != _entityFollowed)
			{
				return;
			}

			SetVignetteIntensity(callback.CurrentHealth, callback.MaxHealth);
		}

		private void OnEventOnHealthIsZero(EventOnHealthIsZero callback)
		{
			if (callback.Entity != _entityFollowed)
			{
				return;
			}

			_vignetteImage.enabled = false;
		}

		private void SetVignetteIntensity(float currentHealth, float maxHealth)
		{
			var healthRatio = currentHealth / (float) maxHealth;
			var newAlpha = Mathf.Clamp01(STARTING_ALPHA +
			                             (NEAR_DEATH_VIGNETTE_HEALTH_RATIO_THRESHOLD - healthRatio) * ALPHA_CHANGE);

			_vignetteImage.enabled = healthRatio < NEAR_DEATH_VIGNETTE_HEALTH_RATIO_THRESHOLD;
			_vignetteImage.color = new Color(1f, 1f, 1f, newAlpha);
		}
	}
}