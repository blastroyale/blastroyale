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
		public void SetupView(Frame f, PlayerCharacter player, EntityRef entity)
		{
			_entity = entity;
			_currentWeapon = player.CurrentWeapon.GameId;
		
			QuantumEvent.Subscribe<EventOnPlayerAmmoChanged>(this, HandleOnPlayerAmmoChanged);
		}

		private void HandleOnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			if (callback.Entity != _entity)
			{
				return;
			}

			_slider.value = (float)callback.CurrentAmmo / callback.MaxAmmo;

			_reloadBarImage.color = _primaryReloadColor;
			
			if (callback.CurrentAmmo <= 0 && _currentWeapon != GameId.Random && _currentWeapon != GameId.Hammer)
			{
				_reloadBarImage.color = _secondaryReloadColor;

				_capacityUsedAnimation.Rewind();
				_capacityUsedAnimation.Play();
			}
		}
	}
}