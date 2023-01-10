using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual components of the LeaderboardEntry elements in the LeaderboardAndRewardsScreen
	/// </summary>
	public class LeaderboardEntryView : IUIView
	{
		private const string UssLeaderboardEntry = "leaderboard-entry";
		private const string UssLeaderboardEntryFirst = UssLeaderboardEntry+"--first";
		private const string UssLeaderboardEntrySecond = UssLeaderboardEntry+"--second";
		private const string UssLeaderboardEntryThird = UssLeaderboardEntry+"--third";
		private const string UssLeaderboardEntryLocal = UssLeaderboardEntry+"--local";
		
		private VisualElement _root;
		private VisualElement _leaderboardEntry;
		private Label _rankNumber;
		private Label _playerName;
		private Label _kills;
		private Label _trophies;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_leaderboardEntry = _root.Q<VisualElement>("LeaderboardEntryParent").Required();
			_rankNumber = _root.Q<Label>("RankNumber").Required();
			_playerName = _root.Q<Label>("PlayerName").Required();
			_kills = _root.Q<Label>("Kills").Required();
			_trophies = _root.Q<Label>("TrophiesAmount").Required();
		}
		
		/// <summary>
		/// Sets the data needed to fill leaderboard entry's data
		/// </summary>
		/// <param name="data">All the match data from the player we're showing</param>
		/// <param name="isLocalPlayer">If this is the local player</param>
		public void SetData(int rank, string playerName, int playerKilledCount, int playerTrophies, bool isLocalPlayer)
		{
			_leaderboardEntry.RemoveModifiers();
			
			if (rank <= 3)
			{
				var rankClass = rank switch
				{
					1 => UssLeaderboardEntryFirst,
					2 => UssLeaderboardEntrySecond,
					3 => UssLeaderboardEntryThird,
					_ => ""
				};
				
				_leaderboardEntry.AddToClassList(rankClass);
			}
			else
			{
				_rankNumber.text = $"{rank.ToString()}.";
			}

			if (isLocalPlayer)
			{
				_leaderboardEntry.AddToClassList(UssLeaderboardEntryLocal);
			}

			_playerName.text = playerName;
			_kills.text = playerKilledCount.ToString();
			_trophies.text = playerTrophies.ToString();

			var delayTime = 0.3f + rank * 0.1f;

			_leaderboardEntry.style.transitionDelay = new List<TimeValue>
			{
				delayTime, delayTime
			};
		}

		public void SubscribeToEvents()
		{
			
		}

		public void UnsubscribeFromEvents()
		{
			
		}
	}
}