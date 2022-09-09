using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Used to display the vignette that indicates to a player about low health
	/// </summary>
	public class NearDeathVignetteView : MonoBehaviour
	{
		private const float MAX_ALPHA_HEALTH_RATIO_THRESHOLD = 0.15f;
		private const float STARTING_ALPHA = 0.25f;
		private const float ALPHA_CHANGE = (1f - STARTING_ALPHA) /
		                                   (GameConstants.Visuals.NEAR_DEATH_HEALTH_RATIO_THRESHOLD -
		                                    MAX_ALPHA_HEALTH_RATIO_THRESHOLD);

		[SerializeField] private Image _vignetteImage;

		private IGameServices _services;
		private IMatchServices _matchServices;

		private void Awake()
		{
			_vignetteImage.enabled = false;
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);

			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnEventOnHealthChanged);
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
			
			SetVignetteIntensity(1f, 1f);
		}

		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			var frame = QuantumRunner.Default.Game.Frames.Predicted;
			var stats = frame.Get<Stats>(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			
			SetVignetteIntensity(stats.CurrentHealth, stats.Values[(int) StatType.Health].StatValue.AsInt);
		}

		private void OnEventOnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}

			SetVignetteIntensity(callback.CurrentHealth, callback.MaxHealth);
		}

		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}

			_vignetteImage.enabled = false;
		}

		private void SetVignetteIntensity(float currentHealth, float maxHealth)
		{
			var healthRatio = currentHealth / (float) maxHealth;
			var newAlpha = Mathf.Clamp01(STARTING_ALPHA +
			                             (GameConstants.Visuals.NEAR_DEATH_HEALTH_RATIO_THRESHOLD - healthRatio) * ALPHA_CHANGE);

			_vignetteImage.enabled = healthRatio < GameConstants.Visuals.NEAR_DEATH_HEALTH_RATIO_THRESHOLD;
			_vignetteImage.color = new Color(1f, 1f, 1f, newAlpha);
		}
	}
}