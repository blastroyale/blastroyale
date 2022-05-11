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
	public class HealthBarInterimArmourView : MonoBehaviour, IPoolEntityDespawn
	{
		public TextMeshProUGUI InterimArmourText;
		public GameObject InterimArmourIcon;
		public Slider ArmourSlider;
		public Image FillImage;

		private EntityRef _entity;

		/// <summary>
		/// Setups this view with the given <paramref name="entity"/> & <paramref name="currentInterimArmour"/>
		/// </summary>
		public void SetupView(EntityRef entity, int currentInterimArmour)
		{
			_entity = entity;
			UpdateInterimArmourText(currentInterimArmour);
			QuantumEvent.Subscribe<EventOnInterimArmourChanged>(this, OnInterimArmourUpdate);
		}
		
		/// <inheritdoc />
		public void OnDespawn()
		{
			_entity = EntityRef.None;
			
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnInterimArmourUpdate(EventOnInterimArmourChanged callback)
		{
			if (callback.Entity == _entity)
			{
				
				UpdateInterimArmourBar((float)callback.CurrentInterimArmour / callback.MaxInterimArmour);
				
			}
		}

		private void UpdateInterimArmourBar(float interimArmour)
		{
			ArmourSlider.value = interimArmour;
			if (!InterimArmourIcon.activeSelf && interimArmour > 0)
			{
				InterimArmourIcon.SetActive(true);
			}
			else if (InterimArmourIcon.activeSelf && interimArmour < float.Epsilon)
			{
				InterimArmourIcon.SetActive(false);
			}
			
		}

		private void UpdateInterimArmourText(int interimArmour)
		{
			InterimArmourText.text = interimArmour.ToString("###0");
			InterimArmourText.gameObject.SetActive(interimArmour > 0);
			InterimArmourIcon.SetActive(interimArmour > 0);
		}
	}
}