using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using Object = UnityEngine.Object;

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
		IObservableFieldReader<TutorialSection> CurrentRunningTutorial { get; }

		/// <summary>
		/// Requests check if a tutorial is currently in progress
		/// </summary>
		bool IsTutorialRunning { get; }

		/// <summary>
		/// Requests to check if a tutorial step has been completed
		/// </summary>
		bool HasCompletedTutorialSection(TutorialSection section);

		/// <summary>
		/// Listen for the first player movement and send PlayerUsedMovementJoystick message
		/// </summary>
		void ListenForSentMovement();
	}

	/// <inheritdoc cref="ITutorialService"/>
	public interface IInternalTutorialService : ITutorialService
	{
		/// <inheritdoc cref="ITutorialService.CurrentRunningTutorial" />
		new IObservableField<TutorialSection> CurrentRunningTutorial { get; }

		/// <summary>
		/// Marks tutorial step completed, to be used at the end of a tutorial sequence
		/// </summary>
		void CompleteTutorialSection(TutorialSection section);

		/// <summary>
		/// Creates first match tutorial room and joins it
		/// </summary>
		void CreateJoinFirstTutorialRoom();

		/// <summary>
		/// Creates second match tutorial room and joins it
		/// </summary>
		void CreateJoinSecondTutorialRoom();

		/// <summary>
		/// Attempts to find a tutorial game object.
		/// </summary>
		GameObject[] FindTutorialObjects(string reference);
	}

	/// <inheritdoc cref="ITutorialService"/>
	public class TutorialService : IInternalTutorialService
	{
		private readonly IGameUiService _uiService;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		bool ITutorialService.IsTutorialRunning => FeatureFlags.TUTORIAL && CurrentRunningTutorial.Value != TutorialSection.NONE;

		public IObservableField<TutorialSection> CurrentRunningTutorial { get; }

		IObservableFieldReader<TutorialSection> ITutorialService.CurrentRunningTutorial => CurrentRunningTutorial;

		public TutorialService(IGameUiService uiService)
		{
			_uiService = uiService;

			CurrentRunningTutorial = new ObservableField<TutorialSection>(TutorialSection.NONE);
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

		public void CompleteTutorialSection(TutorialSection section)
		{
			_services.CommandService.ExecuteCommand(new CompleteTutorialSectionCommand()
			{
				Section = section
			});
		}

		public void CreateJoinFirstTutorialRoom()
		{
			var gameModeId = GameConstants.Tutorial.FIRST_TUTORIAL_GAME_MODE_ID;

			var roomSetup = new MatchRoomSetup()
			{
				GameModeId = gameModeId,
				MapId = GameId.FtueDeck.GetHashCode(),
				RoomIdentifier = Guid.NewGuid().ToString(),
				Mutators = Array.Empty<string>(),
				JoinType = JoinType.ForcedGame,
			};

			_services.NetworkService.CreateRoom(roomSetup, true);
		}

		public void CreateJoinSecondTutorialRoom()
		{
			var gameModeId = GameConstants.Tutorial.SECOND_BOT_MODE_ID;
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);

			var setup = new MatchRoomSetup()
			{
				GameModeId = gameModeId,
				MapId = gameModeConfig.AllowedMaps[0].GetHashCode(),
				RoomIdentifier = Guid.NewGuid().ToString(),
				Mutators = Array.Empty<string>(),
				JoinType = JoinType.ForcedGame,
				MatchType = MatchType.Ranked,
			};

			_services.NetworkService.JoinOrCreateRandomRoom(setup);
		}

		public GameObject[] FindTutorialObjects(string referenceTag)
		{
			var objects = GameObject.FindGameObjectsWithTag(referenceTag);
#if DEVELOPMENT_BUILD
			if (objects == null || objects.Length == 0)
			{
				throw new Exception($"Tutorial could not find game object {referenceTag} - was reference changed ?");
			}
#endif
			return objects;
		}

		public bool HasCompletedTutorialSection(TutorialSection section)
		{
			return _dataProvider.PlayerDataProvider.HasTutorialSection(section);
		}


		public void ListenForSentMovement()
		{
			if (!MainInstaller.TryResolve<IMatchServices>(out var matchServices))
			{
				throw new Exception("MatchServices not found!");
			}

			void OnQuantumInputSent(Quantum.Input input)
			{
				if (CurrentRunningTutorial.Value != TutorialSection.FIRST_GUIDE_MATCH || input.Direction.Magnitude <= FP._0_05) return;
				_services.MessageBrokerService.Publish(new PlayerUsedMovementJoystick());
				matchServices.PlayerInputService.OnQuantumInputSent -= OnQuantumInputSent;

			}

			matchServices.PlayerInputService.OnQuantumInputSent += OnQuantumInputSent;
		}
	}
}