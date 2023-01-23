using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides calls to tutorial related methods, UI, and requesting tutorial status
	/// </summary>
	public interface ITutorialService
	{
		/// <summary>
		/// Requests the current running tutorial step
		/// </summary>
		IObservableFieldReader<TutorialStep> CurrentRunningTutorial { get; }
		
		/// <summary>
		/// Requests check if a tutorial is currently in progress
		/// </summary>
		bool IsTutorialRunning { get; }
	}

	/// <inheritdoc cref="ITutorialService"/>
	public interface IInternalTutorialService : ITutorialService
	{
		/// <inheritdoc cref="ITutorialService.CurrentRunningTutorial" />
		new IObservableField<TutorialStep> CurrentRunningTutorial { get; }
		
		/// <summary>
		/// Marks tutorial step completed, to be used at the end of a tutorial sequence
		/// </summary>
		void CompleteTutorialStep(TutorialStep step);

		/// <summary>
		/// Creates first match tutorial room and joins it
		/// </summary>
		void CreateJoinFirstTutorialRoom();
	}

	/// <inheritdoc cref="ITutorialService"/>
	public class TutorialService : IInternalTutorialService
	{
		private readonly IGameUiService _uiService;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		
		bool ITutorialService.IsTutorialRunning => CurrentRunningTutorial.Value != TutorialStep.NONE;
		
		public IObservableField<TutorialStep> CurrentRunningTutorial { get; }

		IObservableFieldReader<TutorialStep> ITutorialService.CurrentRunningTutorial => CurrentRunningTutorial;
		
		public TutorialService(IGameUiService uiService)
		{
			_uiService = uiService;

			CurrentRunningTutorial = new ObservableField<TutorialStep>(TutorialStep.NONE);
		}
		
		/// <summary>
		/// Binds services and data to the object, and starts starts ticking quantum client.
		/// Done here, instead of constructor because things are initialized in a particular order in Main.cs
		/// </summary>
		public void BindServicesAndData(IGameDataProvider dataProvider, IGameServices services)
		{
			_services = services;
			_dataProvider = dataProvider;
		}
		
		public void CompleteTutorialStep(TutorialStep step)
		{
			_services.CommandService.ExecuteCommand(new CompleteTutorialStepCommand()
			{
				Step = step
			});
		}

		public void CreateJoinFirstTutorialRoom()
		{
			var gameModeId = "BattleRoyale";
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());
			var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(GameId.BRGenesis.GetHashCode());

			_services.NetworkService.CreateRoom(gameModeConfig, mapConfig, new List<string>(), GameConstants.Tutorial.TUTORIAL_ROOM_NAME, false);
		}
	}
}