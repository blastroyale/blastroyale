using System;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles displaying the special buttons and their state.
	/// </summary>
	public class SpecialButtonsView : UIView
	{
		internal SpecialButtonElement _special0Button;
		internal SpecialButtonElement _special1Button;

		/// <summary>
		/// Called with 0f when Special0 starts dragging / presses the button and with 1f when Special0 is released.
		/// </summary>
		public event Action<float> OnSpecial0Pressed;

		/// <summary>
		/// Called with 0f when Special1 starts dragging / presses the button and with 1f when Special1 is released.
		/// </summary>
		public event Action<float> OnSpecial1Pressed;

		/// <summary>
		/// Called with the aiming direction of the special currently being aimed. Not called on non-draggable specials.
		/// </summary>
		public event Action<Vector2> OnDrag;

		/// <summary>
		/// Called when the special is canceled.
		/// </summary>
		public event Action<float> OnCancel;

		private IMatchServices _matchServices;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_special0Button = element.Q<SpecialButtonElement>("Special0").Required();
			_special1Button = element.Q<SpecialButtonElement>("Special1").Required();

			_special0Button.OnPress += val => OnSpecial0Pressed?.Invoke(val);
			_special1Button.OnPress += val => OnSpecial1Pressed?.Invoke(val);
			_special0Button.OnDrag += val => OnDrag?.Invoke(val);
			_special1Button.OnDrag += val => OnDrag?.Invoke(val);
			_special0Button.OnCancel += val => OnCancel?.Invoke(val);
			_special1Button.OnCancel += val => OnCancel?.Invoke(val);

			_matchServices = MainInstaller.ResolveMatchServices();
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpecialUsed>(OnLocalPlayerSpecialUsed);
			QuantumEvent.SubscribeManual<EventOnPlayerSpecialUpdated>(OnPlayerSpecialUpdated);
		}

		private void OnPlayerSpecialUpdated(EventOnPlayerSpecialUpdated callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;

			switch (callback.SpecialIndex)
			{
				case 0:
					UpdateSpecial(callback.Game.Frames.Verified, callback.Special, _special0Button);
					break;
				case 1:
					UpdateSpecial(callback.Game.Frames.Verified, callback.Special, _special1Button);
					break;
			}
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		public void UpdateFromLatestVerifiedFrame()
		{
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();
			var f = QuantumRunner.Default.Game.Frames.Verified;
			var inventory = f.Get<PlayerInventory>(playerEntity);
			UpdateSpecials(f, inventory);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var inventory = callback.Game.Frames.Verified.Get<PlayerInventory>(callback.Entity);
			UpdateSpecials(callback.Game.Frames.Predicted, inventory);
		}

		private void OnLocalPlayerSpecialUsed(EventOnLocalPlayerSpecialUsed callback)
		{
			// TODO: Disable input. Here?
			switch (callback.SpecialIndex)
			{
				case 0:
					// TODO: Callback to enable input
					_special0Button.DisableFor((long) (1000L * callback.Special.Cooldown.AsFloat), null);
					break;
				case 1:
					// TODO: Callback to enable input
					_special1Button.DisableFor((long) (1000L * callback.Special.Cooldown.AsFloat), null);
					break;
			}
		}

		private void UpdateSpecial(Frame f, Special special, SpecialButtonElement button)
		{
			if (special.IsValid)
			{
				button.SetVisibility(true);
				button.SetSpecial(special.SpecialId, special.IsAimable,
					Math.Max(0L, (long) (special.AvailableTime - f.Time).AsFloat * 1000L));
			}
			else
			{
				button.SetSpecial(GameId.Random, false, 0L);
				button.SetVisibility(false);
			}
		}

		private void UpdateSpecials(Frame f, PlayerInventory inventory)
		{
			if (f.Context.TryGetMutatorByType(MutatorType.DoNotDropSpecials, out _))
			{
				_special0Button.SetVisibility(false);
				_special1Button.SetVisibility(false);
				return;
			}

			UpdateSpecial(f, inventory.Specials[0], _special0Button);
			UpdateSpecial(f, inventory.Specials[1], _special1Button);
		}
	}
}