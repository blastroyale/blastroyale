using System.Collections;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This View handles the Resource Bar View in the UI for the player:
	/// - Showing the current resource status of the actor
	/// </summary>
	public class ResourceBarView : MonoBehaviour, IPoolEntityDespawn
	{
		[SerializeField, Required] private Slider _slider;
		[SerializeField, Required] private GameObject _separatorRef;
		[SerializeField, Required] private Animation _capacityUsedAnimation;
		[SerializeField, Required] private Image _reloadBarImage;
		[SerializeField] private Color _primaryReloadColor;
		[SerializeField] private Color _secondaryReloadColor;

		private EntityRef _entity;
		private Coroutine _coroutine;
		private IObjectPool<GameObject> _separatorPool;
		private GameId _currentWeapon;

		/// <inheritdoc />
		public void OnDespawn()
		{
			_entity = EntityRef.None;
			
			QuantumEvent.UnsubscribeListener(this);
		}
		
		/// <summary>
		/// Updates this reload bar be configured to the given <paramref name="entity"/> with the given data
		/// </summary>
		public void SetupView(Frame f, EntityRef entity)
		{
			_entity = entity;
			_currentWeapon = Equipment.None.GameId;
			SetSliderValue(f, entity);
			
			QuantumEvent.Subscribe<EventOnPlayerAmmoChanged>(this, HandleOnPlayerAmmoChanged);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandleOnPlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, HandleOnPlayerAttacked);
		}

		private void HandleOnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (callback.Entity != _entity)
			{
				return;
			}

			_currentWeapon = callback.Weapon.GameId;
			SetSliderValue(callback.Game.Frames.Verified, callback.Entity);
		}

		private void HandleOnPlayerAttacked(EventOnPlayerAttack callback)
		{
			if (callback.PlayerEntity != _entity || !callback.WeaponConfig.IsMeleeWeapon)
			{
				return;
			}

			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}
			
			_coroutine = StartCoroutine(MeleeCooldownCoroutine(callback.WeaponConfig.AttackCooldown.AsFloat));
		}

		private void HandleOnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			if (callback.Entity != _entity)
			{
				return;
			}
			
			// If the weapon is not melee
			if (callback.MaxAmmo > 0)
			{
				_slider.value = callback.CurrentAmmo / (float)callback.MaxAmmo;
			}
			
			_reloadBarImage.color = _primaryReloadColor;

			
			if (callback.CurrentAmmo <= 0 && _currentWeapon != GameId.Random && _currentWeapon != GameId.Hammer)
			{
				_reloadBarImage.color = _secondaryReloadColor;

				_capacityUsedAnimation.Rewind();
				_capacityUsedAnimation.Play();
			}
		}

		private void SetSliderValue(Frame f, EntityRef entity)
		{
			if (!f.TryGet<PlayerCharacter>(entity, out var player))
			{
				return;
			}
			
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