using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using Quantum;

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
			var dead = stateFactory.State("Dead Hud");
			var respawning = stateFactory.State("Respawning");
			var resyncCheck = stateFactory.Choice("Resync Check");
			var aliveCheck = stateFactory.Choice("Alive Check");
			
			initial.Transition().Target(resyncCheck);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(OpenMatchHud);

			resyncCheck.Transition().Condition(IsResyncing).Target(aliveCheck);
			resyncCheck.Transition().Target(countdown);
			
			aliveCheck.Transition().Condition(IsLocalPlayerAlive).Target(alive);
			aliveCheck.Transition().Target(dead);
			aliveCheck.OnExit(SendReadyForResyncMessage);
			
			countdown.OnEnter(ShowCountdownHud);
			countdown.WaitingFor(Countdown).Target(alive);
			countdown.OnExit(PublishMatchStarted);

			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(dead);
			alive.OnExit(CloseControlsHud);

			dead.OnEnter(CloseMatchHud);
			dead.OnEnter(OpenKilledHud);
			dead.Event(_localPlayerAliveEvent).OnTransition(OpenControlsHud).Target(alive);
			dead.Event(_localPlayerRespawnEvent).OnTransition(OpenControlsHud).Target(respawning);
			dead.OnExit(CloseKilledHud);

			respawning.Event(_localPlayerAliveEvent).Target(alive);

			final.OnEnter(CloseMatchHud);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
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

			if (game.Frames.Verified.Has<DeadPlayerCharacter>(localPlayer.Entity))
			{
				return true;
			}
		
			return false;
		}
		
		private bool IsResyncing()
		{
			return !_services.NetworkService.IsJoiningNewRoom;
		}
		
		private void SendReadyForResyncMessage()
		{
			_services.MessageBrokerService.Publish(new MatchReadyForResyncMessage());
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

			// "Key" = Number of times I killed this player, "Value" = number of times that player killed me.
			if (deadData.IsLocalPlayer || killerData.IsLocalPlayer)
			{
				var recordName = deadData.IsLocalPlayer ? killerData.Data.Player : deadData.Data.Player;

				if (!_killsDictionary.TryGetValue(recordName, out var recordPair))
				{
					recordPair = new Pair<int, int>();

					_killsDictionary.Add(recordName, recordPair);
				}

				recordPair.Key += deadData.IsLocalPlayer ? 0 : 1;
				recordPair.Value += deadData.IsLocalPlayer ? 1 : 0;

				_killsDictionary[recordName] = recordPair;
			}
		}

		private void PublishMatchStarted()
		{
			_killsDictionary.Clear();
			_services.MessageBrokerService.Publish(new MatchStartedMessage());
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
				OnRespawnClicked = OnRespawnClicked
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

		private void OnRespawnClicked()
		{
			_statechartTrigger(_localPlayerRespawnEvent);
		}
	}
}