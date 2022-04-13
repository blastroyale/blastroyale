using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View holds the information to show the actor's health  
	/// </summary>
	public class HealthBarTextView : MonoBehaviour, IPoolEntityDespawn
	{
		public TextMeshProUGUI HealthText;

		private EntityRef _entity;

		/// <summary>
		/// Setups this view with the given <paramref name="entity"/> & <paramref name="currentHealth"/>
		/// </summary>
		public void SetupView(EntityRef entity, int currentHealth)
		{
			_entity = entity;
			
			UpdateHealthText(currentHealth);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnHealthUpdate);
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (callback.Entity == _entity)
			{
				UpdateHealthText(callback.CurrentHealth);
			}
		}

		/// <inheritdoc />
		public void OnDespawn()
		{
			_entity = EntityRef.None;
			
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnHealthUpdate(EventOnHealthChanged callback)
		{
			if (callback.Entity == _entity)
			{
				UpdateHealthText(callback.CurrentHealth);
			}
		}
		
		private void UpdateHealthText(int health)
		{
			HealthText.text = health.ToString("###0");
		}
	}
}