using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
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
		
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private PlayerRef _killer;
		
		public BattleRoyaleState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService,
		                         Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
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
			var spectator = stateFactory.Wait("Spectator Hud");
			var spawning = stateFactory.State("Spawning");

			initial.Transition().Target(spawning);
			initial.OnExit(SubscribeEvents);
			
			spawning.OnEnter(OpenAdventureHud);
			spawning.Event(_localPlayerAliveEvent).Target(alive);
			spawning.OnExit(PublishMatchStarted);
			
			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(spectator);
			alive.OnExit(CloseControlsHud);

			spectator.OnEnter(CloseAdventureHud);
			spectator.WaitingFor(SpectatorHud).Target(final);
			spectator.OnExit(CloseSpectatorHud);
			
			final.OnEnter(CloseAdventureHud);
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
		
		private void OpenAdventureHud()
		{
			_uiService.OpenUi<MatchHudPresenter>();
		}

		private void CloseAdventureHud()
		{
			_uiService.CloseUi<MatchHudPresenter>();
		}
		
		private void PublishMatchStarted()
		{
			_services.MessageBrokerService.Publish(new MatchStartedMessage());
		}
		
		private void SpectatorHud(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new BattleRoyaleSpectatorHudPresenter.StateData
			{
				Killer = _killer,
				OnLeaveClicked = LeaveClicked
			};
			
			_uiService.OpenUi<BattleRoyaleSpectatorHudPresenter, BattleRoyaleSpectatorHudPresenter.StateData>(data);

			void LeaveClicked()
			{
				cacheActivity.Complete();
			}
		}
		
		private void CloseSpectatorHud()
		{
			_uiService.CloseUi<BattleRoyaleSpectatorHudPresenter>();
		}
	}
}