using System;
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
		private VisualElement _root;
		private VisualElement _parent;
		private Label _rankNumber;
		private Label _playerName;
		private Label _kills;
		private Label _deaths;
		private Label _trophies;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_parent = _root.Q<VisualElement>("LeaderboardEntryParent").Required();
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
			if (data.PlayerRank <= 3)
			{
				var rankClass = data.PlayerRank switch
				{
					1 => "first",
					2 => "second",
					3 => "third",
					_ => ""
				};
				
				_root.AddToClassList(rankClass);
			}
			else
			{
				_rankNumber.text = $"{data.PlayerRank.ToString()}.";
			}

			if (isLocalPlayer)
			{
				_root.AddToClassList("local");
			}

			_playerName.text = data.GetPlayerName();
			_kills.text = data.Data.PlayersKilledCount.ToString();
			_deaths.text = data.Data.DeathCount.ToString();
			_trophies.text = data.Data.PlayerTrophies.ToString();

			_parent.style.marginRight = 50 + (data.PlayerRank - 1) * 10;
		}

		public void SubscribeToEvents()
		{
			
		}

		public void UnsubscribeFromEvents()
		{
			
		}
	}
}