using System.Collections;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This View handles the Health Bar View in the UI:
	/// - Showing the current Health status of the actor
	/// </summary>
	public class ReloadBarView : MonoBehaviour, IPoolEntityDespawn
	{
		[SerializeField, Required] private Slider _slider;
		[SerializeField, Required] private GameObject _separatorRef;
		[SerializeField, Required] private Animation _capacityUsedAnimation;
		[SerializeField, Required] private Image _reloadBarImage;
		[SerializeField] private Color _primaryReloadColor;
		[SerializeField] private Color _secondaryReloadColor;
		
		private Coroutine _coroutine;
		private IObjectPool<GameObject> _separatorPool;

		/// <inheritdoc />
		public void OnDespawn()
		{
			QuantumEvent.UnsubscribeListener(this);
		}
		
		/// <summary>
		/// Updates this reload bar be configured to the given <paramref name="entity"/> with the given data
		/// </summary>
		public void SetupView(Frame f, EntityRef entity)
		{
			SetSliderValue(f, entity);
			
			QuantumEvent.Subscribe<EventOnLocalPlayerAmmoChanged>(this, HandleOnPlayerAmmoChanged);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, HandleOnPlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnLocalPlayerAttack>(this, HandleOnPlayerAttacked);
		}

		private void HandleOnPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSliderValue(callback.Game.Frames.Verified, callback.Entity);
		}

		private void HandleOnPlayerAttacked(EventOnLocalPlayerAttack callback)
		{
			if (!callback.WeaponConfig.IsMeleeWeapon)
			{
				return;
			}

			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}
			
			_coroutine = StartCoroutine(MeleeCooldownCoroutine(callback.WeaponConfig.AttackCooldown.AsFloat));
		}

		private void HandleOnPlayerAmmoChanged(EventOnLocalPlayerAmmoChanged callback)
		{
			// If the weapon is not melee
			if (callback.MaxAmmo > 0)
			{
				_slider.value = callback.CurrentAmmo / (float)callback.MaxAmmo;
			}
			
			_reloadBarImage.color = _primaryReloadColor;

			if (callback.CurrentAmmo <= 0)
			{
				_reloadBarImage.color = _secondaryReloadColor;

				_capacityUsedAnimation.Rewind();
				_capacityUsedAnimation.Play();
			}
		}

		private void SetSliderValue(Frame f, EntityRef entity)
		{
			var player = f.Get<PlayerCharacter>(entity);
			
			_slider.value = player.HasMeleeWeapon(f, entity) ? 1f : player.GetAmmoAmountFilled(f, entity).AsFloat;
			_reloadBarImage.color = _primaryReloadColor;
		}

		private IEnumerator MeleeCooldownCoroutine(float cooldown)
		{
			var endTime = Time.time + cooldown;

			while (Time.time < endTime)
			{
				_slider.value = Mathf.Lerp(1, 0, (endTime - Time.time) / cooldown);

				yield return null;
			}

			_slider.value = 1f;
			_coroutine = null;
		}
	}
}