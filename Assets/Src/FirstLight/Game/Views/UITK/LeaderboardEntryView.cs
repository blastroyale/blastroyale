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
		private Label _deaths;
		private Label _trophies;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_leaderboardEntry = _root.Q<VisualElement>("LeaderboardEntryParent").Required();
			_rankNumber = _root.Q<Label>("RankNumber").Required();
			_playerName = _root.Q<Label>("PlayerName").Required();
			_kills = _root.Q<Label>("Kills").Required();
			_deaths = _root.Q<Label>("Deaths").Required();
			_trophies = _root.Q<Label>("TrophiesAmount").Required();
		}
		
		/// <summary>
		/// Sets the data needed to fill leaderboard entry's data
		/// </summary>
		/// <param name="data">All the match data from the player we're showing</param>
		/// <param name="isLocalPlayer">If this is the local player</param>
		public void SetData(QuantumPlayerMatchData data, bool isLocalPlayer)
		{
			_leaderboardEntry.RemoveModifiers();
			
			if (data.PlayerRank <= 3)
			{
				var rankClass = data.PlayerRank switch
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
				_rankNumber.text = $"{data.PlayerRank.ToString()}.";
			}

			if (isLocalPlayer)
			{
				_leaderboardEntry.AddToClassList(UssLeaderboardEntryLocal);
			}

			_playerName.text = data.GetPlayerName();
			_kills.text = data.Data.PlayersKilledCount.ToString();
			_deaths.text = data.Data.DeathCount.ToString();
			_trophies.text = data.Data.PlayerTrophies.ToString();
		}

		public void SubscribeToEvents()
		{
			
		}

		public void UnsubscribeFromEvents()
		{
			
		}
	}
}