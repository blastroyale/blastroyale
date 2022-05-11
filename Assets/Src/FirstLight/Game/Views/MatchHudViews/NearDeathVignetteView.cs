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
		
		private EntityRef Entity { get; set; }

		private void Awake()
		{
			_vignetteImage.enabled = false;
			
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnEventOnHealthChanged, onlyIfActiveAndEnabled : true);
			QuantumEvent.Subscribe<EventOnHealthIsZero>(this, OnEventOnHealthIsZero, onlyIfActiveAndEnabled : true);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
		}
		
		private void OnEventOnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != Entity)
			{
				return;
			}

			var healthRatio = callback.CurrentHealth / (float) callback.MaxHealth;
			var newAlpha = Mathf.Clamp01(STARTING_ALPHA + (NEAR_DEATH_VIGNETTE_HEALTH_RATIO_THRESHOLD - healthRatio) * ALPHA_CHANGE);

			_vignetteImage.enabled = healthRatio < NEAR_DEATH_VIGNETTE_HEALTH_RATIO_THRESHOLD;
			_vignetteImage.color = new Color(1f,1f,1f, newAlpha);
		}

		private void OnEventOnHealthIsZero(EventOnHealthIsZero callback)
		{
			if (callback.Entity != Entity)
			{
				return;
			}
			
			_vignetteImage.enabled = false;
		}
		
		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			Entity = callback.Entity;
			_vignetteImage.enabled = false;
		}
	}
}
