using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This View holds the information to show the actor's interim armour  
	/// </summary>
	public class HealthBarShieldView : MonoBehaviour, IPoolEntityDespawn
	{
		public GameObject ShieldIcon;
		public Slider ArmourSlider;

		private EntityRef _entity;

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnShieldChanged>(this, OnShieldUpdate);
		}

		/// <summary>
		/// Setups this view with the given <paramref name="entity"/> & <paramref name="currentShield"/>
		/// </summary>
		public void SetupView(EntityRef entity, int currentShield)
		{
			_entity = entity;
			
			ShieldIcon.SetActive(currentShield > 0);
		}
		
		/// <inheritdoc />
		public void OnDespawn()
		{
			_entity = EntityRef.None;
		}

		private void OnShieldUpdate(EventOnShieldChanged callback)
		{
			if (callback.Entity == _entity)
			{
				UpdateShieldBar((float)callback.CurrentShield / callback.ShieldCapacity);
			}
		}

		private void UpdateShieldBar(float shield)
		{
			if (!ShieldIcon.activeSelf && shield > 0)
			{
				ShieldIcon.SetActive(true);
			}
			else if (ShieldIcon.activeSelf && shield < float.Epsilon)
			{
				ShieldIcon.SetActive(false);
			}
			
		}
	}
}