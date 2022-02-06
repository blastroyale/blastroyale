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
		private int _maxAmmo;

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
		public void SetupView(Frame f, EntityRef entity)
		{
			_maxAmmo = f.Get<Weapon>(entity).MaxAmmo;
			_entity = entity;
			_separatorPool ??= new GameObjectPool(10, _separatorRef);

			SetSliderValue(f);
			
			QuantumEvent.Subscribe<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
			QuantumEvent.Subscribe<EventOnPlayerAmmoChanged>(this, HandleOnPlayerAmmoChanged);
		}

		private void HandleOnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			SetSliderValue(callback.Game.Frames.Verified);
		}

		private void SetSliderValue(Frame f)
		{
			var playerCharacter = f.Get<PlayerCharacter>(_entity);
			var ammo = playerCharacter.GetAmmoAmount(f, _entity);
			var isMelee = playerCharacter.IsMeleeWeapon(f, _entity);
			
			_slider.value = isMelee ? 1f : (float) ammo / _maxAmmo;
			_reloadBarImage.color = _primaryReloadColor;
		}
		
		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			if (callback.Entity != _entity)
			{
				return;
			}

			_reloadBarImage.color = _secondaryReloadColor;
			
			_capacityUsedAnimation.Rewind();
			_capacityUsedAnimation.Play();
		}
	}
}