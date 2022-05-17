using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View holds the information to show the actor's interim armour  
	/// </summary>
	public class HealthBarShieldView : MonoBehaviour, IPoolEntityDespawn
	{
		public TextMeshProUGUI ShieldText;
		public GameObject ShieldIcon;
		public Slider ShieldSlider;
		public Image FillImage;

		private EntityRef _entity;

		/// <summary>
		/// Setups this view with the given <paramref name="entity"/> & <paramref name="currentShields"/>
		/// </summary>
		public void SetupView(EntityRef entity, int currentShields)
		{
			_entity = entity;
			UpdateShieldText(currentShields);
			QuantumEvent.Subscribe<EventOnShieldChanged>(this, OnShieldUpdate);
		}
		
		/// <inheritdoc />
		public void OnDespawn()
		{
			_entity = EntityRef.None;
			
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnShieldUpdate(EventOnShieldChanged callback)
		{
			if (callback.Entity == _entity)
			{
				UpdateShieldBar((float)callback.CurrentShield / callback.ShielcCapacity);
			}
		}

		private void UpdateShieldBar(float shields)
		{
			ShieldSlider.value = shields;
			if (!ShieldIcon.activeSelf && shields > 0)
			{
				ShieldIcon.SetActive(true);
			}
			else if (ShieldIcon.activeSelf && shields < float.Epsilon)
			{
				ShieldIcon.SetActive(false);
			}
			
		}

		private void UpdateShieldText(int shields)
		{
			ShieldText.text = shields.ToString("###0");
			ShieldText.gameObject.SetActive(shields > 0);
			ShieldIcon.SetActive(shields > 0);
		}
	}
}