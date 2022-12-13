using System;
using FirstLight.FLogger;
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
		private readonly IStatechartEvent _localPlayerExitEvent = new StatechartEvent("Local Player Exit");
		private readonly IStatechartEvent _localPlayerNextEvent = new StatechartEvent("Local Player Next");

		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly Action _leftMatchBeforeFinishCallback;

		private PlayerRef _killer;

		public BattleRoyaleState(IGameServices services, IGameUiService uiService,
								 Action<IStatechartEvent> statechartTrigger, Action leftMatchBeforeFinishCallback)
		{
			_services = services;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_leftMatchBeforeFinishCallback = leftMatchBeforeFinishCallback;
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
			var resyncAliveCheck = stateFactory.Choice("Resync Alive Check");

			initial.Transition().Target(spectateCheck);
			initial.OnExit(SubscribeEvents);

			spectateCheck.Transition().Condition(IsSpectator).Target(spectating);
			spectateCheck.Transition().Target(resyncCheck);

			resyncCheck.OnEnter(OpenMatchHud);
			resyncCheck.Transition().Condition(IsRejoining).Target(resyncAliveCheck);
			resyncCheck.Transition().Target(spawning);

			resyncAliveCheck.Transition().Condition(IsLocalPlayerAlive).Target(alive);
			resyncAliveCheck.Transition().Target(deadCheck);

			spawning.Event(_localPlayerAliveEvent).Target(alive);

			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(deadCheck);
			alive.OnExit(CloseControlsHud);

			deadCheck.Transition().Condition(IsMatchEnding).Target(final);
			deadCheck.Transition().Target(dead);

			dead.OnEnter(MatchEndAnalytics);
			dead.OnEnter(CloseMatchHud);
			dead.OnEnter(OpenMatchEndScreen);
			dead.Event(_localPlayerNextEvent).Target(spectating);

			spectating.OnEnter(OpenSpectateScreen);
			spectating.Event(_localPlayerExitEvent).OnTransition(SetQuitDuringSpectate).Target(final);

			final.OnEnter(CloseMatchHud);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}
		
		private void MatchEndAnalytics()
		{
			if (IsSpectator())
			{
				return;
			}

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var matchData = gameContainer.GetPlayersMatchData(f, out _);
			var localPlayerData = matchData[game.GetLocalPlayers()[0]];
			var totalPlayers = 0;

			for (var i = 0; i < matchData.Count; i++)
			{
				if (matchData[i].Data.IsValid && !f.Has<BotCharacter>(matchData[i].Data.Entity))
				{
					totalPlayers++;
				}
			}
   
			_services.AnalyticsService.MatchCalls.MatchEnd(totalPlayers, false, f.Time.AsFloat, localPlayerData);
		}
		
		private void SetQuitDuringSpectate()
		{
			_leftMatchBeforeFinishCallback();
		}

		private bool IsMatchEnding()
		{
			var f = QuantumRunner.Default.Game.Frames.Verified;
			return f.GetSingleton<GameContainer>().IsGameOver;
		}

		private bool IsLocalPlayerAlive()
		{
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			return localPlayer.Entity.IsAlive(f);
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}

		private bool IsRejoining()
		{
			return !_services.NetworkService.IsJoiningNewMatch;
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			_statechartTrigger(_localPlayerAliveEvent);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_killer = callback.PlayerKiller;

			_statechartTrigger(_localPlayerDeadEvent);
		}

		private void OpenControlsHud()
		{
			_uiService.OpenUi<MatchControlsHudPresenter>();
		}

		private void CloseControlsHud()
		{
			_uiService.CloseUi<MatchControlsHudPresenter>();
		}

		private void OpenMatchHud()
		{
			_uiService.OpenUi<MatchHudPresenter>();
		}

		private void CloseMatchHud()
		{
			_uiService.CloseUi<MatchHudPresenter>();
		}

		private void OpenMatchEndScreen()
		{
			var data = new MatchEndScreenPresenter.StateData
			{
				Killer = _killer,
				OnNextClicked = () => _statechartTrigger(_localPlayerNextEvent),
			};

			_uiService.OpenScreen<MatchEndScreenPresenter, MatchEndScreenPresenter.StateData>(data);
		}

		private void OpenSpectateScreen()
		{
			var data = new SpectateScreenPresenter.StateData
			{
				Killer = _killer,
				OnLeaveClicked = () => _statechartTrigger(_localPlayerExitEvent)
			};

			_uiService.OpenScreen<SpectateScreenPresenter, SpectateScreenPresenter.StateData>(data);

			_services.MessageBrokerService.Publish(new SpectateStartedMessage());
		}
	}
}