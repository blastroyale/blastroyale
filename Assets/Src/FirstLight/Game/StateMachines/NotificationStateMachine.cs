using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// The State Machine that controls the entire flow of the OS notifications of the game.
	/// It heavily uses <see cref="NotificationService"/>
	/// </summary>
	public class NotificationStateMachine
	{
		private const int _startDefaultIndex = -100;
		
		private readonly IStatechartEvent _notificationsAllowedEvent = new StatechartEvent("Notifications Allowed Event");
		private readonly IStatechartEvent _mainMenuLoaded = new StatechartEvent("Main Menu Loaded");
		
		private readonly IStatechart _statechart;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameServices _services;

		/// <inheritdoc cref="IStatechart.LogsEnabled"/>
		public bool LogsEnabled
		{
			get => _statechart.LogsEnabled;
			set => _statechart.LogsEnabled = value;
		}

		public NotificationStateMachine(IGameDataProvider dataProvider, IGameServices services)
		{
			_dataProvider = dataProvider;
			_services = services;
			_statechart = new Statechart.Statechart(Setup);
		}

		/// <inheritdoc cref="IStatechart.Run"/>
		public void Run()
		{
			_statechart.Run();
		}

		private void Trigger(IStatechartEvent eventTrigger)
		{
			_statechart.Trigger(eventTrigger);
		}

		private void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var notificationWaitingCheck = stateFactory.State("Notification Waiting Check");
			var notificationCheck = stateFactory.Choice("Notification Status Check");
			var notificationsWaitingApproval = stateFactory.State("Notifications Waiting for approval");
			var notificationApprovalPopUp = stateFactory.TaskWait("Notification Approval Popup");
			var notificationsActive = stateFactory.State("Notifications Active");
			
			initial.Transition().Target(notificationWaitingCheck);
			initial.OnExit(SubscribeEvents);
			
			notificationWaitingCheck.Event(_mainMenuLoaded).Target(notificationCheck);
			
			notificationCheck.Transition().Condition(IsNotificationsOn).Target(notificationsActive);
			notificationCheck.Transition().Target(notificationsWaitingApproval);
			
			notificationsWaitingApproval.Event(_notificationsAllowedEvent).Target(notificationApprovalPopUp);
			
			notificationApprovalPopUp.WaitingFor(_services.NotificationService.RequestPermissions).Target(notificationCheck);
			
			notificationsActive.OnEnter(SubscribeActiveEvents);
			notificationsActive.OnEnter(_services.NotificationService.DismissAllDisplayedNotifications);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<LootBoxCollectedAllMessage>(OnAllLootBoxCollected);
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpened);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private void SubscribeActiveEvents()
		{
			_services.MessageBrokerService.Subscribe<ApplicationPausedMessage>(OnApplicationPaused);
			_services.MessageBrokerService.Subscribe<LootBoxUnlockingMessage>(OnLootBoxUnlocking);
			_services.MessageBrokerService.Subscribe<LootBoxHurryCompletedMessage>(OnLootBoxHurryCompleted);
		}

		private bool IsNotificationsOn()
		{
			return _services.NotificationService.IsNotificationsOn;
		}
		
		private void OnPlayScreenOpened(PlayScreenOpenedMessage obj)
		{
			_statechart.Trigger(_mainMenuLoaded);
		}
		
		private void OnAllLootBoxCollected(LootBoxCollectedAllMessage message)
		{
			_statechart.Trigger(_notificationsAllowedEvent);
		}

		private void OnApplicationPaused(ApplicationPausedMessage message)
		{
			if (message.IsPaused)
			{
				SetupIdleLootBoxNotifications();
			}
			else
			{
				_services.NotificationService.DismissAllDisplayedNotifications();
			}
		}

		private void OnLootBoxHurryCompleted(LootBoxHurryCompletedMessage message)
		{
			_services.NotificationService.CancelNotification((int) message.LootBoxId.Id);
		}

		private void OnLootBoxUnlocking(LootBoxUnlockingMessage message)
		{
			var notification = _services.NotificationService.CreateNotification();
			var info = _dataProvider.LootBoxDataProvider.GetTimedBoxInfo(message.LootBoxId);
			
			notification.Id = (int) info.Data.Id.Id;
			notification.Title = ScriptLocalization.Notifications.CrateReadyTitle;
			notification.Channel = GameConstants.NotificationBoxesChannel;
			notification.Body = string.Format(ScriptLocalization.Notifications.CrateReady, info.Config.LootBoxId.GetTranslation());
			notification.DeliveryTime = _services.TimeService.ConvertToLocalTime(info.Data.EndTime);
			
			// _services.NotificationService.ScheduleNotification(notification); //TODO: Reenable when Crates are added back to the game.
		}

		private void SetupIdleLootBoxNotifications()
		{
			var notificationService = _services.NotificationService;
			var time = _services.TimeService.DateTimeUtcNow;
			var info = _dataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			var id = _startDefaultIndex;

			foreach (var notification in notificationService.PendingNotifications)
			{
				if (notification.Notification.Channel == GameConstants.NotificationIdleBoxesChannel)
				{
					notificationService.CancelNotification(notification.Notification.Id.Value);
				}
			}
			
			foreach (var box in info.TimedBoxSlots)
			{
				if (!box.HasValue || box.Value.GetState(time) != LootBoxState.Unlocked)
				{
					continue;
				}
				
				var notification = notificationService.CreateNotification();

				notification.Id = --id;
				notification.Channel = GameConstants.NotificationIdleBoxesChannel;
				notification.Title = ScriptLocalization.Notifications.UnopenedCrateTitle;
				notification.Body = ScriptLocalization.Notifications.UnopenedCrate;
				notification.DeliveryTime = _services.TimeService.ConvertToLocalTime(time.AddMinutes(5));
					
				notificationService.ScheduleNotification(notification);
				return;
			}

			if (!info.LootBoxUnlocking.HasValue && info.GetSlotsFilledCount() > 0)
			{
				var notification = notificationService.CreateNotification();

				notification.Id = --id;
				notification.Channel = GameConstants.NotificationIdleBoxesChannel;
				notification.Title = ScriptLocalization.Notifications.NoCrateUnlockingTitle;
				notification.Body = ScriptLocalization.Notifications.NoCrateUnlocking;
				notification.DeliveryTime = _services.TimeService.ConvertToLocalTime(time.AddMinutes(5));
					
				notificationService.ScheduleNotification(notification);
			}
		}
	}
}