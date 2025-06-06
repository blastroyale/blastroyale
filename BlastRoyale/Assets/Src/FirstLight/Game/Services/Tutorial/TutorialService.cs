using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
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
		/// Requests check if a tutorial is currently in progress
		/// </summary>
		bool IsTutorialRunning { get; }

		/// <summary>
		/// Requests to check if a tutorial step has been completed
		/// </summary>
		bool HasCompletedTutorialSection(TutorialSection section);

		/// <summary>
		/// If the player has completed the entire tutorial.
		/// </summary>
		/// <returns></returns>
		bool HasCompletedTutorial();

		/// <summary>
		/// Requests the current running tutorial step
		/// </summary>
		IObservableField<TutorialSection> CurrentRunningTutorial { get; }

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

		/// <summary>
		/// Skips the tutorial for the current player, it runs a command to change player data
		/// </summary>
		void SkipTutorial();
	}

	/// <inheritdoc cref="ITutorialService"/>
	public interface IInternalTutorialService : ITutorialService
	{
	}

	/// <inheritdoc cref="ITutorialService"/>
	public class TutorialService : IInternalTutorialService
	{
		private IRoomService _roomService;
		private IGameCommandService _commandService;
		private IGameDataProvider _dataProvider;
		private IConfigsProvider _configsProvider;

		bool ITutorialService.IsTutorialRunning => CurrentRunningTutorial.Value != TutorialSection.NONE;

		public IObservableField<TutorialSection> CurrentRunningTutorial { get; }

		IObservableField<TutorialSection> ITutorialService.CurrentRunningTutorial => CurrentRunningTutorial;

		public TutorialService(IRoomService roomService, IGameCommandService commandService, IConfigsProvider configsProvider,
							   IGameDataProvider dataProvider)
		{
			_roomService = roomService;
			_commandService = commandService;
			_dataProvider = dataProvider;
			_configsProvider = configsProvider;
			CurrentRunningTutorial = new ObservableField<TutorialSection>(TutorialSection.NONE);
		}

		public void CompleteTutorialSection(TutorialSection section)
		{
			if (HasCompletedTutorialSection(section)) return;
			_commandService.ExecuteCommand(new CompleteTutorialSectionCommand()
			{
				Sections = new[] {section}
			});
		}

		public void CreateJoinFirstTutorialRoom()
		{
			var gameModeId = GameConstants.Tutorial.FIRST_TUTORIAL_GAME_MODE_ID;

			var roomSetup = new MatchRoomSetup()
			{
				SimulationConfig = new SimulationMatchConfig()
				{
					GameModeID = gameModeId,
					MapId = GameId.FtueDeck.ToString(),
					MatchType = MatchType.Forced,
					TeamSize = 1,
					DisableBots = false
				},
				RoomIdentifier = Guid.NewGuid().ToString(),
			};

			_roomService.CreateRoom(roomSetup, true);
		}

		public void CreateJoinSecondTutorialRoom()
		{
			// Normal play button behaviour will handle it
			if (!FeatureFlags.TUTORIAL_BATTLE) return;

			var config = _configsProvider.GetConfig<TutorialConfig>().SecondMatch.CloneSerializing();
			config.MatchType = MatchType.Forced;
			var setup = new MatchRoomSetup()
			{
				SimulationConfig = config,
				RoomIdentifier = Guid.NewGuid().ToString(),
			};

			_roomService.JoinOrCreateRoom(setup);
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
			return !FeatureFlags.TUTORIAL || _dataProvider.PlayerDataProvider.HasTutorialSection(section);
		}

		public bool HasCompletedTutorial()
		{
			return HasCompletedTutorialSection(TutorialSection.META_GUIDE_AND_MATCH) &&
				HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH);
		}

		public void SkipTutorial()
		{
			_commandService.ExecuteCommand(new CompleteTutorialSectionCommand()
			{
				Sections = new[]
				{
					TutorialSection.FIRST_GUIDE_MATCH,
					TutorialSection.META_GUIDE_AND_MATCH,
				}
			});
		}
	}
}