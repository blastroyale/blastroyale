using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Battle Royale's game State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class BattleRoyaleState
	{
		private readonly IStatechartEvent _localPlayerDeadEvent = new StatechartEvent("Local Player Dead");
		private readonly IStatechartEvent _localPlayerAliveEvent = new StatechartEvent("Local Player Alive");
		private readonly IStatechartEvent _localPlayerSpectateEvent = new StatechartEvent("Local Player Spectate");
		private readonly IStatechartEvent _localPlayerExitEvent = new StatechartEvent("Local Player Exit");
		
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private PlayerRef _killer;

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
			var aliveCheck = stateFactory.Choice("Alive Check");
			
			initial.Transition().Target(spectateCheck);
			initial.OnExit(SubscribeEvents);
			
			spectateCheck.Transition().Condition(IsSpectator).Target(spectating);
			spectateCheck.Transition().Target(resyncCheck);
			
			resyncCheck.OnEnter(OpenMatchHud);
			resyncCheck.Transition().Condition(IsRejoining).Target(aliveCheck);
			resyncCheck.Transition().Target(spawning);
			
			aliveCheck.Transition().Condition(IsLocalPlayerAlive).Target(alive);
			aliveCheck.Transition().Target(deadCheck);

			spawning.Event(_localPlayerAliveEvent).Target(alive);
			spawning.OnExit(CloseMatchmakingScreen);

			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(deadCheck);
			alive.OnExit(CloseControlsHud);
			
			deadCheck.Transition().Condition(IsMatchEnding).Target(final);
			deadCheck.Transition().Target(dead);

			dead.OnEnter(CloseMatchHud);
			dead.OnEnter(OpenKillScreen);
			dead.Event(_localPlayerExitEvent).Target(final);
			dead.Event(_localPlayerSpectateEvent).Target(spectating);
			dead.OnExit(CloseKillScreen);

			spectating.OnEnter(OpenMatchHud);
			spectating.OnEnter(OpenSpectateHud);
			spectating.Event(_localPlayerExitEvent).Target(final);
			spectating.OnExit(CloseSpectateHud);
			spectating.OnExit(CloseMatchHud);
			
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

		private void OpenKillScreen()
		{
			var data = new BattleRoyaleDeadScreenPresenter.StateData
			{
				Killer = _killer,
				OnLeaveClicked = () => { _statechartTrigger(_localPlayerExitEvent); },
				OnSpectateClicked = () => { _statechartTrigger(_localPlayerSpectateEvent); }
			};

			_uiService.OpenUiAsync<BattleRoyaleDeadScreenPresenter, BattleRoyaleDeadScreenPresenter.StateData>(data);
		}

		private void CloseKillScreen()
		{ 
			_uiService.CloseUi<BattleRoyaleDeadScreenPresenter>(false, true);
		}

		private async void OpenSpectateHud()
		{
			await _uiService.OpenUiAsync<SpectateHudPresenter>();
			
			_services.MessageBrokerService.Publish(new SpectateStartedMessage());
		}

		private void CloseSpectateHud()
		{
			_uiService.CloseUi<SpectateHudPresenter>();
		}
		
		private void CloseMatchmakingScreen()
		{
			_uiService.CloseUi<MatchmakingLoadingScreenPresenter>(false, true);
		}
	}
}