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
			var dead = stateFactory.State("Dead Screen");
			var spectating = stateFactory.State("Spectate Screen");
			var spawning = stateFactory.State("Spawning");
			var resyncCheck = stateFactory.Choice("Resync Check");
			var aliveCheck = stateFactory.Choice("Alive Check");
			
			initial.Transition().Target(resyncCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(OpenMatchHud);

			resyncCheck.Transition().Condition(IsResyncing).Target(aliveCheck);
			resyncCheck.Transition().Target(spawning);
			resyncCheck.OnExit(PublishMatchStartedMessage);
			
			aliveCheck.Transition().Condition(IsLocalPlayerAlive).Target(alive);
			aliveCheck.Transition().Target(dead);

			spawning.Event(_localPlayerAliveEvent).Target(alive);

			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(dead);
			alive.OnExit(CloseControlsHud);

			dead.OnEnter(CloseMatchHud);
			dead.OnEnter(OpenKillScreen);
			dead.Event(_localPlayerExitEvent).Target(final);
			dead.Event(_localPlayerSpectateEvent).Target(spectating);
			dead.OnExit(CloseKillScreen);

			spectating.OnEnter(OpenSpectateScreen);
			spectating.Event(_localPlayerExitEvent).Target(final);
			spectating.OnExit(CloseSpectateScreen);

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
		
		private bool IsLocalPlayerAlive()
		{
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			return localPlayer.Entity.IsAlive(f);
		}
		
		private bool IsResyncing()
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

			_uiService.OpenUi<BattleRoyaleDeadScreenPresenter, BattleRoyaleDeadScreenPresenter.StateData>(data);
		}

		private void CloseKillScreen()
		{
			_uiService.CloseUi<BattleRoyaleDeadScreenPresenter>();
		}

		private void OpenSpectateScreen()
		{
			var data = new BattleRoyaleSpectateScreenPresenter.StateData
			{
				OnLeaveClicked = () => { _statechartTrigger(_localPlayerExitEvent); }
			};

			_uiService.OpenUi<BattleRoyaleSpectateScreenPresenter, BattleRoyaleSpectateScreenPresenter.StateData>(data);
			
			_services.MessageBrokerService.Publish(new SpectateKillerMessage());
		}

		private void CloseSpectateScreen()
		{
			_uiService.CloseUi<BattleRoyaleSpectateScreenPresenter>();
		}

		private void PublishMatchStartedMessage()
		{
			_services.MessageBrokerService.Publish(new MatchStartedMessage() { IsResync = IsResyncing()});
		}
	}
}