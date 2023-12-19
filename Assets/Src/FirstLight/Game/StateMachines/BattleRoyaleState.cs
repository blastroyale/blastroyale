using System;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Battle Royale's game State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class BattleRoyaleState
	{
		private readonly IStatechartEvent _localPlayerDeadEvent = new StatechartEvent("Local Player Dead");
		private readonly IStatechartEvent _localPlayerAliveEvent = new StatechartEvent("Local Player Alive");
		private readonly IStatechartEvent _localPlayerNextEvent = new StatechartEvent("Local Player Next");

		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private IMatchServices _matchServices;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public BattleRoyaleState(IGameServices services, IGameUiService uiService,
								 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Battle Royale gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var alive = stateFactory.State("Alive Hud");
			var deadCheck = stateFactory.Choice("Dead Check");
			var dead = stateFactory.State("Dead Screen");
			var spectating = stateFactory.State("Spectate Screen");
			var spawning = stateFactory.State("Spawning");
			var resyncCheck = stateFactory.Choice("Resync Check");
			var spectateCheck = stateFactory.Choice("Spectate Check");
			var resyncChecks = stateFactory.Choice("Resync Checks");

			initial.Transition().Target(spectateCheck);
			initial.OnExit(SubscribeEvents);

			spectateCheck.Transition().Condition(IsNotOnline).Target(final);
			spectateCheck.Transition().Condition(IsSpectator).Target(spectating);
			spectateCheck.Transition().Target(resyncCheck);

			resyncCheck.OnEnter(OpenMatchHud);
			resyncCheck.Transition().Condition(IsNotOnline).Target(final);
			resyncCheck.Transition().Condition(IsRejoining).Target(resyncChecks);
			resyncCheck.Transition().Target(spawning);

			resyncChecks.Transition().Condition(IsNotOnline).Target(final);
			resyncChecks.Transition().Condition(IsLocalPlayerAlive).Target(alive);
			resyncChecks.Transition().Target(deadCheck);

			spawning.Event(NetworkState.PhotonDisconnectedEvent).Target(final);
			spawning.Event(_localPlayerAliveEvent).Target(alive);
			
			alive.Event(_localPlayerDeadEvent).Target(deadCheck);
			alive.Event(NetworkState.PhotonDisconnectedEvent).Target(final);

			deadCheck.Transition().Condition(IsNotOnline).Target(final);
			deadCheck.Transition().Condition(IsMatchEnding).Target(final);
			deadCheck.Transition().Target(dead);
			
			dead.OnEnter(CloseMatchHud);
			dead.OnEnter(MatchEndAnalytics);
			dead.OnEnter(OpenMatchEndScreen);
			dead.Event(_localPlayerNextEvent).Target(spectating);
			dead.Event(NetworkState.PhotonDisconnectedEvent).Target(final);
			dead.OnExit(() =>
			{
				_uiService.CloseUi<MatchEndScreenPresenter>();
			});
			
			spectating.OnEnter(OpenSpectateScreen);
			spectating.Event(GameSimulationState.LocalPlayerExitEvent).Target(final);
			spectating.Event(NetworkState.PhotonDisconnectedEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}
		
		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();;
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}
		
		private bool IsNotOnline()
		{
			return !_services.NetworkService.QuantumClient.IsConnectedAndReady;
		}

		
		private void MatchEndAnalytics()
		{
			_services.AnalyticsService.MatchCalls.MatchEndBRPlayerDead(QuantumRunner.Default.Game, _matchServices.MatchEndDataService.LocalPlayerMatchData.PlayerRank);
		}

		public bool IsMatchEnding()
		{
			var f = QuantumRunner.Default.Game.Frames.Verified;
			var container = f.GetSingleton<GameContainer>();
			return container.IsGameOver || container.IsGameCompleted;
		}

		private bool IsLocalPlayerAlive()
		{
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayerRef()];

			return localPlayer.Entity.IsAlive(f);
		}

		private bool IsSpectator()
		{
			return _services.RoomService.IsLocalPlayerSpectator || QuantumUtils.IsLocalPlayerNotPresent();
		}

		private bool IsRejoining()
		{
			return _services.NetworkService.JoinSource.HasResync();
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			_statechartTrigger(_localPlayerAliveEvent);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_statechartTrigger(_localPlayerDeadEvent);
		}

		private void CloseMatchHud()
		{
			_uiService.CloseUi<HUDScreenPresenter>();
		}
		
		private void OpenMatchHud()
		{
			_uiService.OpenScreen<HUDScreenPresenter>();
		}
		
		private void OpenMatchEndScreen()
		{
			var data = new MatchEndScreenPresenter.StateData
			{
				OnTimeToLeave = () =>
				{
					_statechartTrigger(_localPlayerNextEvent);
				},
			};

			_uiService.OpenScreen<MatchEndScreenPresenter, MatchEndScreenPresenter.StateData>(data);
		}

		private void OpenSpectateScreen()
		{
			var data = new SpectateScreenPresenter.StateData
			{
				OnLeaveClicked = () =>
				{
					_services.MessageBrokerService.Publish(new LeftBeforeMatchFinishedMessage());
					_statechartTrigger(GameSimulationState.LocalPlayerExitEvent);
				}
			};

			_uiService.OpenScreen<SpectateScreenPresenter, SpectateScreenPresenter.StateData>(data);

			_services.MessageBrokerService.Publish(new SpectateStartedMessage());
		}
	}
}