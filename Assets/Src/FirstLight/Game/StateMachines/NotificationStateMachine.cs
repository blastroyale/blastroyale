using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Statechart;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// The State Machine that controls the entire flow of the OS notifications of the game.
	/// It heavily uses <see cref="NotificationService"/>
	/// </summary>
	public class NotificationStateMachine
	{
		private const int _startDefaultIndex = -100;

		private readonly IStatechartEvent _notificationsAllowedEvent =
			new StatechartEvent("Notifications Allowed Event");

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

			notificationApprovalPopUp.WaitingFor(_services.NotificationService.RequestPermissions)
			                         .Target(notificationCheck);

			notificationsActive.OnEnter(SubscribeActiveEvents);
			notificationsActive.OnEnter(_services.NotificationService.DismissAllDisplayedNotifications);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpened);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private void SubscribeActiveEvents()
		{
			_services.MessageBrokerService.Subscribe<ApplicationPausedMessage>(OnApplicationPaused);
		}

		private bool IsNotificationsOn()
		{
			return _services.NotificationService.IsNotificationsOn;
		}

		private void OnPlayScreenOpened(PlayScreenOpenedMessage obj)
		{
			_statechart.Trigger(_mainMenuLoaded);
		}

		private void OnApplicationPaused(ApplicationPausedMessage message)
		{
			if (!message.IsPaused)
			{
				_services.NotificationService.DismissAllDisplayedNotifications();
			}
		}
	}
}