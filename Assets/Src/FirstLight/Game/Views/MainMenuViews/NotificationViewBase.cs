using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Abstract class that handles Notifications in game, e.g. if the player finds new Equipment, new Game Modes, etc.
	/// </summary>
	public abstract class NotificationViewBase : MonoBehaviour
	{
		[SerializeField] protected Image NotificationImage;
		[SerializeField] protected Animation NotificationAnimation;
		
		[SerializeField] protected TextMeshProUGUI _notificationText;

		protected IGameDataProvider DataProvider;
		protected IGameServices Services;

		/// <summary>
		/// Requests the notification text to be shown
		/// </summary>
		public TextMeshProUGUI NotificationText => _notificationText;

		/// <summary>
		/// Requests the notification visual state. Returns true if is shown, false otherwise
		/// </summary>
		public bool State => NotificationImage.enabled;

		protected virtual void Start()
		{
			DataProvider = MainInstaller.Resolve<IGameDataProvider>();
			Services = MainInstaller.Resolve<IGameServices>();
		}

		/// <summary>
		/// Updates the notification view state
		/// </summary>
		public virtual void UpdateState() {}
		
		/// <summary>
		/// Set's the notification to the given <paramref name="state"/> and plays the animation if <paramref name="playAnimation"/>
		/// is marked as true
		/// </summary>
		public virtual void SetNotificationState(bool state, bool playAnimation = true)
		{
			NotificationImage.enabled = state;
			NotificationText.enabled = state;

			if (state && playAnimation)
			{
				NotificationAnimation.Rewind();
				NotificationAnimation.Play();
			}
		}
	}
	
	/// <inheritdoc />
	/// <remarks>
	/// Inherit this view base for new item notifications
	/// </remarks>
	public abstract class NotificationNewViewBase : NotificationViewBase
	{
		protected override void Start()
		{
			base.Start();
			DataProvider.UniqueIdDataProvider.NewIds.Observe(OnUniqueIdChanged);
		}
		
		protected void OnDestroy()
		{
			DataProvider?.UniqueIdDataProvider?.NewIds?.StopObserving(OnUniqueIdChanged);
		}

		protected abstract void OnUniqueIdChanged(int id, UniqueId uniqueId, UniqueId change, ObservableUpdateType updateType);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Inherit this view base for upgrade item notifications
	/// </remarks>
	public abstract class NotificationUpgradeViewBase : NotificationViewBase
	{
		protected override void Start()
		{
			base.Start();
			DataProvider.CurrencyDataProvider.Currencies.Observe(OnCurrencyChanged);
		}
		
		protected void OnDestroy()
		{
			DataProvider?.CurrencyDataProvider?.Currencies?.StopObserving(OnCurrencyChanged);
		}

		protected abstract void OnCurrencyChanged(GameId currency, ulong newAmount, ulong change, ObservableUpdateType updateType);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Inherit this view base for time based tickable notifications notifications
	/// </remarks>
	public abstract class NotificationTickableViewBase : NotificationViewBase
	{
		private readonly Dictionary<int, Coroutine> _coroutines = new Dictionary<int, Coroutine>();
		
		private int _counter = 0;
		
		protected void OnDestroy()
		{
			StopCoroutines();
			OnDestroyVirtual();
		}
		
		protected virtual void OnDestroyVirtual() {}

		protected void StopCoroutines()
		{
			foreach (var pair in _coroutines)
			{
				Services.CoroutineService.StopCoroutine(pair.Value);
			}
		}

		protected void SetTimeState(DateTime endTime)
		{
			var id = _counter++;
			
			_coroutines.Add(id, Services.CoroutineService.StartCoroutine(TimeCoroutine(endTime, id)));
		}

		private IEnumerator TimeCoroutine(DateTime endTime, int id)
		{
			var waiter = new WaitForSeconds(1);
			var time = endTime - Services.TimeService.DateTimeUtcNow;

			while (time.Ticks > 0)
			{
				yield return waiter;
				
				time = endTime - Services.TimeService.DateTimeUtcNow;
			}

			_coroutines.Remove(id);
			UpdateState();
		}
	}
}
