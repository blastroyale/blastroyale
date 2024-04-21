using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
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

		public TutorialService(IRoomService roomService, IGameCommandService commandService, IConfigsProvider configsProvider, IGameDataProvider dataProvider)
		{
			_roomService = roomService;
			_commandService = commandService;
			_dataProvider = dataProvider;
			_configsProvider = configsProvider;
			CurrentRunningTutorial = new ObservableField<TutorialSection>(TutorialSection.NONE);
		}

		public void CompleteTutorialSection(TutorialSection section)
		{
			_commandService.ExecuteCommand(new CompleteTutorialSectionCommand()
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
				MatchType = MatchType.Forced,
				AllowedRewards = new ()
			};

			_roomService.CreateRoom(roomSetup, true);
		}

		public void CreateJoinSecondTutorialRoom()
		{
			var gameModeId = GameConstants.Tutorial.SECOND_BOT_MODE_ID;
			var gameModeConfig = _configsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);

			var rewards = GameConstants.Data.AllowedGameRewards.ToList();
			rewards.Remove(GameId.NOOB);
			var setup = new MatchRoomSetup()
			{
				GameModeId = gameModeId,
				MapId = gameModeConfig.AllowedMaps[0].GetHashCode(),
				RoomIdentifier = Guid.NewGuid().ToString(),
				Mutators = Array.Empty<string>(),
				MatchType = MatchType.Forced,
				AllowedRewards = rewards
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
	}
}