using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the game's app
	/// </summary>
	public interface IAppDataProvider
	{
		/// <summary>
		/// Requests the information if the current game session is the first time the player is playing the game or not
		/// </summary>
		bool IsFirstSession { get; }

		/// <summary>
		/// Requests the information if the game was or not yet reviewed
		/// </summary>
		bool IsGameReviewed { get; }
		
		/// <summary>
		/// Returns the last ranked map user has selected
		/// </summary>
		int LastSelectedRankedMap { get; set; }
		
		/// <summary>
		/// Gets last current custom game options used
		/// </summary>
		CustomGameOptions LastCustomGameOptions { get; }
		
		/// <summary>
		/// Marks the date when the game was last time reviewed
		/// </summary>
		void MarkGameAsReviewed();

		/// <summary>
		/// Last time player snapshotted a frame
		/// </summary>
		IObservableField<FrameSnapshot> LastFrameSnapshot { get; }
	}

	/// <inheritdoc cref="IAppLogic"/>
	public interface IAppLogic : IAppDataProvider
	{
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class AppLogic : AbstractBaseLogic<AppData>, IAppLogic
	{
		private readonly DateTime _defaultZeroTime = new (2020, 1, 1);
		
		/// <inheritdoc />
		public bool IsFirstSession => Data.IsFirstSession;

		/// <inheritdoc />
		public bool IsGameReviewed => Data.GameReviewDate > _defaultZeroTime;

		/// <inheritdoc />
		public CustomGameOptions LastCustomGameOptions => Data.LastCustomGameOptions;

		public int LastSelectedRankedMap
		{
			get => Data.LastSelectedRankedMap;
			set => Data.LastSelectedRankedMap = value;
		}

		public IObservableField<FrameSnapshot> LastFrameSnapshot { get; private set; }
		
		public AppLogic(IGameLogic gameLogic, IDataProvider dataProvider) :
			base(gameLogic, dataProvider)
		{
			LastFrameSnapshot = new ObservableResolverField<FrameSnapshot>(() => Data.LastCapturedFrameSnapshot,
				snap => Data.LastCapturedFrameSnapshot = snap);
		}

		public void SetLastCustomGameOptions(CustomGameOptions options)
		{
			Data.LastCustomGameOptions = options;
		}

		/// <inheritdoc />
		public void MarkGameAsReviewed()
		{
			if (IsGameReviewed)
			{
				throw new LogicException("The game was already reviewed and cannot be reviewed again");
			}

			Data.GameReviewDate = GameLogic.TimeService.DateTimeUtcNow;
		}

		// TODO mihak: Make sure that tags are handled again
		// public string GetDisplayName(bool trimmed = true, bool tagged = true)
		// {
		// 	var name = DisplayName == null || string.IsNullOrWhiteSpace(DisplayName.Value) ||
		// 		DisplayName.Value.Length < 5
		// 			? ""
		// 			: trimmed
		// 				? DisplayName.Value.Substring(0, DisplayName.Value.Length - 5)
		// 				: DisplayName.Value;
		//
		// 	if (tagged)
		// 	{
		// 		var playerData = DataProvider.GetData<PlayerData>();
		// 		var skin = TemporaryPlayerBadges.GetBadgeBasedOnFlags(playerData.Flags);
		// 		return skin != null ? $"{skin.GetSpriteText()} {name}" : name;
		// 	}
		//
		// 	return name;
		// }
	}
}
