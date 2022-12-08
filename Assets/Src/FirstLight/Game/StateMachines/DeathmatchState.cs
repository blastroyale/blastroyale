using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
	/// This object contains the behaviour logic for the Deathmatch's game State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class DeathmatchState
	{
		private readonly IStatechartEvent _localPlayerDeadEvent = new StatechartEvent("Local Player Dead");
		private readonly IStatechartEvent _localPlayerRespawnEvent = new StatechartEvent("Local Player Respawn");
		private readonly IStatechartEvent _localPlayerAliveEvent = new StatechartEvent("Local Player Alive");
		private readonly IStatechartEvent _localPlayerExitEvent = new StatechartEvent("Local Player Exit");

		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly Dictionary<PlayerRef, Pair<int, int>> _killsDictionary = new();

		public DeathmatchState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService,
		                       Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Deathmatch gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var countdown = stateFactory.TaskWait("Countdown Hud");
			var alive = stateFactory.State("Alive Hud");
			var deadCheck = stateFactory.Choice("Dead Check");
			var dead = stateFactory.State("Dead Hud");
			var spectating = stateFactory.State("Spectate Screen");
			var respawning = stateFactory.State("Respawning");
			var resyncCheck = stateFactory.Choice("Resync Check");
			var spectateCheck = stateFactory.Choice("Spectate Check");
			var aliveCheck = stateFactory.Choice("Alive Check");
			
			initial.Transition().Target(spectateCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(_killsDictionary.Clear);

			spectateCheck.Transition().Condition(IsSpectator).Target(spectating);
			spectateCheck.Transition().Target(resyncCheck);
			
			resyncCheck.OnEnter(OpenMatchHud);
			resyncCheck.Transition().Condition(IsRejoining).Target(aliveCheck);
			resyncCheck.Transition().Target(countdown);
			
			aliveCheck.Transition().Condition(IsLocalPlayerAlive).Target(alive);
			aliveCheck.Transition().Target(dead);

			countdown.OnEnter(ShowCountdownHud);
			countdown.WaitingFor(Countdown).Target(alive);

			alive.OnEnter(OpenMatchHud);
			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(deadCheck);
			alive.OnExit(CloseControlsHud);

			deadCheck.Transition().Condition(IsMatchEnding).Target(final);
			deadCheck.Transition().Target(dead);
			
			dead.OnEnter(CloseMatchHud);
			dead.OnEnter(OpenKilledHud);
			// Needed on transition in case the player leaves the game in the dead screen
			dead.Event(_localPlayerAliveEvent).OnTransition(OpenControlsHud).Target(alive);
			dead.Event(_localPlayerRespawnEvent).OnTransition(OpenControlsHud).Target(respawning);
			dead.OnExit(CloseKilledHud);
			
			spectating.OnEnter(OpenMatchHud);
			spectating.OnEnter(OpenSpectateHud);
			spectating.Event(_localPlayerExitEvent).Target(final);
			spectating.OnExit(CloseSpectateHud);
			spectating.OnExit(CloseMatchHud);

			respawning.Event(_localPlayerAliveEvent).Target(alive);

			final.OnEnter(CloseMatchHud);
			final.OnEnter(CloseControlsHud);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
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
			var localPlayer = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);
			
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
			_statechartTrigger(_localPlayerDeadEvent);
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var killerData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerKiller));
			var deadData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerDead));

			var frameContext = callback.Game.Frames.Verified.Context;
			var deadLocalPlayer = frameContext.IsLocalPlayer(deadData.Data.Player);
			var killerLocalPlayer = frameContext.IsLocalPlayer(killerData.Data.Player);
			
			// "Key" = Number of times I killed this player, "Value" = number of times that player killed me.
			if (deadLocalPlayer || killerLocalPlayer)
			{
				var recordName = deadLocalPlayer ? killerData.Data.Player : deadData.Data.Player;

				if (!_killsDictionary.TryGetValue(recordName, out var recordPair))
				{
					recordPair = new Pair<int, int>();

					_killsDictionary.Add(recordName, recordPair);
				}

				recordPair.Key += deadLocalPlayer ? 0 : 1;
				recordPair.Value += deadLocalPlayer ? 1 : 0;

				_killsDictionary[recordName] = recordPair;
			}
		}
		
		private void OpenSpectateHud()
		{
			_uiService.OpenScreen<SpectateScreenPresenter, SpectateScreenPresenter.StateData>(new SpectateScreenPresenter.StateData());

			_services.MessageBrokerService.Publish(new SpectateStartedMessage());
		}

		private void CloseSpectateHud()
		{
			_uiService.CloseUi<SpectateScreenPresenter>();
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

		private void OpenKilledHud()
		{
			var data = new DeathmatchDeadScreenPresenter.StateData
			{
				KillerData = _killsDictionary,
			};

			_uiService.OpenUi<DeathmatchDeadScreenPresenter, DeathmatchDeadScreenPresenter.StateData>(data);
		}

		private void CloseKilledHud()
		{
			_uiService.CloseUi<DeathmatchDeadScreenPresenter>();
		}

		private async Task Countdown()
		{
			await Task.Delay(3000);
		}

		private void ShowCountdownHud()
		{
			_uiService.OpenUi<GameCountdownScreenPresenter>();
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			if (callback.HasRespawned)
			{
				_statechartTrigger(_localPlayerRespawnEvent);
			}
		}
	}
}