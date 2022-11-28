using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This View handles the Reload Bar View in the UI for the player:
	/// - Showing the current status of the magazine and reload progress
	/// </summary>
	public class ReloadBarView : MonoBehaviour, IPoolEntityDespawn
	{
		[SerializeField, Required] private Slider _slider;
		[SerializeField, Required] private GameObject _separatorRef;
		[SerializeField, Required] private Slider _reloadTimeSlider;

		private Coroutine _reloadAnim;
		private EntityRef _entity;
		private IObjectPool<GameObject> _separatorPool;

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
			SetSliderValue(f, player);
			
			QuantumEvent.Subscribe<EventOnPlayerAmmoChanged>(this, HandleOnPlayerAmmoChanged);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandleOnPlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnPlayerStopAttack>(this, HandleOnPlayerStopAttack);
		}

		private void HandleOnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			var f = callback.Game.Frames.Verified;
			
			if (callback.Entity != _entity || !f.TryGet<PlayerCharacter>(callback.Entity, out var player))
			{
				return;
			}

			SetSliderValue(f, player);

			if (_reloadAnim != null)
			{
				StopCoroutine(_reloadAnim);
			}
			_reloadAnim = StartCoroutine(ReloadAnimation(f, player));
		}

		private void HandleOnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			var f = callback.Game.Frames.Verified;

			if (callback.Entity != _entity || !f.TryGet<PlayerCharacter>(callback.Entity, out var player))
			{
				return;
			}

			if (_reloadAnim != null)
			{
				StopCoroutine(_reloadAnim);
			}

			SetSliderValue(f, player);
		}

		private void HandleOnPlayerStopAttack(EventOnPlayerStopAttack callback)
		{
			var f = callback.Game.Frames.Verified;
			if (callback.PlayerEntity != _entity || !f.TryGet<PlayerCharacter>(callback.PlayerEntity, out var player))
			{
				return;
			}

			if (_reloadAnim != null)
			{
				StopCoroutine(_reloadAnim);
			}
			_reloadAnim = StartCoroutine(ReloadAnimation(f, player));
		}

		private void SetSliderValue(Frame f, PlayerCharacter player)
		{
			var slot = player.WeaponSlots[player.CurrentWeaponSlot];
			_slider.value = (float)slot.MagazineShotCount / slot.MagazineSize;
			_reloadTimeSlider.value = 0;
			_reloadTimeSlider.gameObject.SetActive(false);
		}

		IEnumerator ReloadAnimation(Frame f, PlayerCharacter player)
		{
			var slot = player.WeaponSlots[player.CurrentWeaponSlot];
			var magShotCount = slot.MagazineShotCount;
			var magSize = slot.MagazineSize;
			var reloadTime = slot.ReloadTime.AsFloat;

			if (magShotCount == magSize || magShotCount < 0 || reloadTime == 0)
			{
				yield break;
			}

			_reloadTimeSlider.gameObject.SetActive(true);
			float startTime = f.Time.AsFloat;
			float endTime = startTime + reloadTime;

			while (f.Time.AsFloat < endTime)
			{
				yield return new WaitForEndOfFrame();
				_reloadTimeSlider.value = (f.Time.AsFloat - startTime) / reloadTime;
			}

			_slider.value = magSize;
			_reloadTimeSlider.gameObject.SetActive(false);
		}
	}

}