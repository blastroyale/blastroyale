using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the settings menu in the <seealso cref="MainMenuState"/>
	/// </summary>
	public class AudioBattleRoyaleState
	{
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public AudioBattleRoyaleState(IGameServices services, IGameDataProvider gameLogic, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = gameLogic;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("AUDIO BR - Initial");
			var final = stateFactory.Final("AUDIO BR - Final");
			var lowIntensity = stateFactory.State("AUDIO BR - Low Intensity");

			initial.Transition().Target(lowIntensity);
			initial.OnExit(SubscribeEvents);


			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}
	}
}