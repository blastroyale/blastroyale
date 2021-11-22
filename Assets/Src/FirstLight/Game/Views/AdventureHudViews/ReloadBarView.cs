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
		/// Updates this reload bar be configured to the given <paramref name="entity"/> with the given <paramref name="projectileCapacity"/>
		/// </summary>
		public void SetupView(EntityRef entity, uint projectileCapacity, uint minProjectileCapacityToShoot)
		{
			_entity = entity;
			_slider.value = 1f;

			if (_separatorPool == null)
			{
				_separatorPool ??= new GameObjectPool(3, _separatorRef);
			
				var separatorsAmount = minProjectileCapacityToShoot == 1
					? projectileCapacity
					: projectileCapacity / minProjectileCapacityToShoot;
				
				separatorsAmount = separatorsAmount > GameConstants.MAX_RELOAD_BAR_SEPARATORS_AMOUNT
					                   ? 0
					                   : separatorsAmount;
				
				for (var i = 1; i < separatorsAmount; i++)
				{
					_separatorPool.Spawn();
				}
			
				QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
				QuantumEvent.Subscribe<EventOnLocalPlayerFailedShoot>(this, HandleOnLocalPlayerFailedShoot);
			}
		}
		
		private void OnUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Verified;
			
			if (frame.TryGet<Weapon>(_entity, out var weapon))
			{
				// If weapon is fully reloaded then we set slider to max
				if (weapon.Capacity >= weapon.MaxCapacity && _slider.value < 1f)
				{
					_slider.value = 1f;
					_reloadBarImage.color = weapon.Emptied ? _secondaryReloadColor : _primaryReloadColor;
					
					return;
				}
				
				// If weapon isn't full then we do the whole process of slider value calculation
				if (weapon.Capacity < weapon.MaxCapacity)
				{
					var reloadFill = (float) weapon.Capacity / weapon.MaxCapacity;
					
					// If weapon has any kind of constant reloading then we take reload time into account
					if (weapon.ReloadType != ReloadType.Never)
					{
						var delta = weapon.NextCapacityIncreaseTime - frame.Time;
						var nextCapacityTimePart = 1f - (delta / weapon.OneCapacityReloadingTime).AsFloat;
						reloadFill += nextCapacityTimePart / weapon.MaxCapacity;
					}
					
					_slider.value = Mathf.Clamp01(reloadFill);
					_reloadBarImage.color = weapon.Emptied ? _secondaryReloadColor : _primaryReloadColor;
				}
			}
		}
		
		private void HandleOnLocalPlayerFailedShoot(EventOnLocalPlayerFailedShoot callback)
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