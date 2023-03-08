using DG.Tweening;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Newtonsoft.Json;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This View handles the Health Bar View in the UI:
	/// - Showing the current Health status of the actor
	/// </summary>
	public class HealthBarView : MonoBehaviour, IPoolEntityDespawn
	{
		[SerializeField, Required] private Slider _damageDealtSlider;
		[SerializeField, Required] private Slider _slider;
		[SerializeField, Required] private Image _fillImage;
		[SerializeField, Required] private TextMeshProUGUI LevelText;
		[SerializeField, Required] private GameObject LevelObj;
		[SerializeField] private Color _normalColor = Color.green;
		[SerializeField] private Color _dangerColor = Color.red;
		[SerializeField, Required] private UnityEvent _healthIncreasedEvent;
		[SerializeField, Required] private Image _damageBlockedIcon;
		[SerializeField] private float _damageBlockedDuration = 2f;
		

		/// <summary>
		/// Requests the entity that this health bar represents
		/// </summary>
		public EntityRef Entity { get; private set; }

		/// <summary>
		/// Event invoked everytime the health is updated for this health bar
		/// </summary>
		public UnityEvent<int, int, int> OnHealthUpdatedEvent { get; } = new();

		private void OnValidate()
		{
			_slider = GetComponent<Slider>();
		}

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnHealthUpdate);
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.Subscribe<EventOnStatusModifierSet>(this, OnStatusModifierSet);
			QuantumEvent.Subscribe<EventOnStatusModifierFinished>(this, OnStatusModifierFinished);
			QuantumEvent.Subscribe<EventOnStatusModifierCancelled>(this, OnStatusModifierCancelled);
			QuantumEvent.Subscribe<EventOnDamageBlocked>(this, OnDamageBlocked);
			QuantumEvent.Subscribe<EventOnPlayerLevelUp>(this, OnPlayerLevelUp);
		}

		/// <summary>
		/// Setups the health bar to be configured to the given <paramref name="entity"/>
		/// with the given <paramref name="currentHealth"/> & <paramref name="maxHealth"/>
		/// </summary>
		public void SetupView(EntityRef entity, int currentHealth, int maxHealth)
		{
			Entity = entity;
			_slider.value = (float)currentHealth / maxHealth;
			_damageDealtSlider.value = (float)currentHealth / maxHealth;

			_damageBlockedIcon.DOKill();
			_damageDealtSlider.DOKill();
			_slider.DOKill();
			HealthBarUpdate();
			LevelObj.SetActive(false);
		}

		/// <inheritdoc />
		public void OnDespawn()
		{
			Entity = EntityRef.None;
		}

		private void OnPlayerLevelUp(EventOnPlayerLevelUp callback)
		{
			if (callback.Entity != Entity)
			{
				return;
			}
			LevelObj.SetActive(true);
			//TODO: play some cool animation or particle effect or something
			LevelText.text = callback.CurrentLevel.ToString();
		}
		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (callback.Entity != Entity)
			{
				return;
			}

			_slider.value = (float) callback.CurrentHealth / callback.MaxHealth;
			_damageDealtSlider.value = (float) callback.CurrentHealth / callback.MaxHealth;
			
			HealthBarUpdate();
			gameObject.SetActive(true);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (callback.Entity != Entity)
			{
				return;
			}

			gameObject.SetActive(false);
		}

		private void OnStatusModifierSet(EventOnStatusModifierSet callback)
		{
			TryHandleInvisibility(callback.Game.Frames.Verified, callback.Entity, callback.Type, false);
		}

		private void OnStatusModifierFinished(EventOnStatusModifierFinished callback)
		{
			TryHandleInvisibility(callback.Game.Frames.Verified, callback.Entity, callback.Type, true);
		}

		private void OnStatusModifierCancelled(EventOnStatusModifierCancelled callback)
		{
			TryHandleInvisibility(callback.Game.Frames.Verified, callback.Entity, callback.Type, true);
		}

		private void OnHealthUpdate(EventOnHealthChanged callback)
		{
			if (callback.Entity != Entity)
			{
				return;
			}

			_slider.DOValue((float)callback.CurrentHealth / callback.MaxHealth, 0.1f).OnUpdate(HealthBarUpdate);
			_damageDealtSlider.DOValue((float)callback.CurrentHealth / callback.MaxHealth, 1f);
			OnHealthUpdatedEvent.Invoke(callback.PreviousHealth, callback.CurrentHealth, callback.MaxHealth);
			_healthIncreasedEvent?.Invoke();
		}

		private void HealthBarUpdate()
		{
			_fillImage.color = _slider.value < GameConstants.Visuals.NEAR_DEATH_HEALTH_RATIO_THRESHOLD
				                   ? _dangerColor
				                   : _normalColor;
		}

		private void OnDamageBlocked(EventOnDamageBlocked callback)
		{
			if (callback.Entity != Entity) return;

			_damageBlockedIcon.DOKill();

			_damageBlockedIcon.color = Color.white;
			_damageBlockedIcon.DOFade(0, 0.3f).SetDelay(_damageBlockedDuration);
		}

		// We switch HealthBar on/off only for enemies affected by Invisibility status modifier
		private void TryHandleInvisibility(Frame f, EntityRef entity, StatusModifierType statusModType, bool newState)
		{
			if (entity != Entity || statusModType != StatusModifierType.Invisibility ||
			    f.Has<PlayerCharacter>(Entity))
			{
				return;
			}

			gameObject.SetActive(newState);
		}
	}
}