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
		public Slider ArmourSlider;
		public Image FillImage;

		private EntityRef _entity;

		/// <summary>
		/// Setups this view with the given <paramref name="entity"/> & <paramref name="currentShield"/>
		/// </summary>
		public void SetupView(EntityRef entity, int currentShield)
		{
			_entity = entity;
			UpdateShieldText(currentShield);
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
				UpdateShieldBar((float)callback.CurrentShield / callback.ShieldCapacity);
			}
		}

		private void UpdateShieldBar(float shield)
		{
			ArmourSlider.value = shield;
			if (!ShieldIcon.activeSelf && shield > 0)
			{
				ShieldIcon.SetActive(true);
			}
			else if (ShieldIcon.activeSelf && shield < float.Epsilon)
			{
				ShieldIcon.SetActive(false);
			}
			
		}

		private void UpdateShieldText(int shield)
		{
			ShieldText.text = shield.ToString("###0");
			ShieldText.gameObject.SetActive(shield > 0);
			ShieldIcon.SetActive(shield > 0);
		}
	}
}