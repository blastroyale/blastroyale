using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View handles the Health Bar View in the UI:
	/// - Showing the current Health status of the actor
	/// </summary>
	public class ReloadBarView : MonoBehaviour, IPoolEntityDespawn
	{
		[SerializeField] private Slider _slider;
		[SerializeField] private GameObject _separatorRef;
		[SerializeField] private Animation _capacityUsedAnimation;
		[SerializeField] private Image _reloadBarImage;
		[SerializeField] private Color _primaryReloadColor;
		[SerializeField] private Color _secondaryReloadColor;
		
		private EntityRef _entity;
		private IObjectPool<GameObject> _separatorPool;

		/// <inheritdoc />
		public void OnDespawn()
		{
			_entity = EntityRef.None;
			
			_separatorPool?.DespawnAll();
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}
		
		/// <summary>
		/// Updates this reload bar be configured to the given <paramref name="entity"/> with the given data
		/// </summary>
		public void SetupView(EntityRef entity, int maxAmmo)
		{
			_entity = entity;
			_slider.value = 1f;

			if (_separatorPool == null)
			{
				_separatorPool ??= new GameObjectPool(3, _separatorRef);
				
				maxAmmo = maxAmmo > GameConstants.MAX_RELOAD_BAR_SEPARATORS_AMOUNT ? 0 : maxAmmo;
				
				for (var i = 1; i < maxAmmo; i++)
				{
					_separatorPool.Spawn();
				}
			
				QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
				QuantumEvent.Subscribe<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
			}
		}
		
		private void OnUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Verified;
			
			if (frame.TryGet<Weapon>(_entity, out var weapon))
			{
				// If weapon is fully reloaded then we set slider to max
				if (weapon.Ammo >= weapon.MaxAmmo && _slider.value < 1f)
				{
					_slider.value = 1f;
					_reloadBarImage.color = weapon.Ammo == 0 ? _secondaryReloadColor : _primaryReloadColor;
					
					return;
				}
				
				// If weapon isn't full then we do the whole process of slider value calculation
				if (weapon.Ammo < weapon.MaxAmmo)
				{
					var reloadFill = (float) weapon.Ammo / weapon.MaxAmmo;
					
					_slider.value = Mathf.Clamp01(reloadFill);
					_reloadBarImage.color = weapon.Ammo == 0 ? _secondaryReloadColor : _primaryReloadColor;
				}
			}
		}
		
		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			if (callback.Entity != _entity)
			{
				return;
			}
			
			_capacityUsedAnimation.Rewind();
			_capacityUsedAnimation.Play();
		}
	}
}