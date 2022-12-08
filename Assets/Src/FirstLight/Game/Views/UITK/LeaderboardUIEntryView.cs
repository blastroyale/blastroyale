using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual components of the LeaderboardEntry elements in the LeaderboardAndRewardsScreen
	/// </summary>
	public class LeaderboardUIEntryView : IUIView
	{
		private const string UssLeaderboardEntry = "leaderboard-entry";
		private const string UssLeaderboardEntryFirst = UssLeaderboardEntry+"--first";
		private const string UssLeaderboardEntrySecond = UssLeaderboardEntry+"--second";
		private const string UssLeaderboardEntryThird = UssLeaderboardEntry+"--third";
		private const string UssLeaderboardEntryLocal = UssLeaderboardEntry+"--local";
		private const string PlayerHighlight = "leaderboard-entry__player-highlight";
		private const string PlayerHighlightHidden = "leaderboard-entry__player-highlight-hidden";
		
		private VisualElement _root;
		private VisualElement _leaderboardUIEntry;
		private VisualElement _localPlayerHighlight;
		private Label _rankNumber;
		private Label _playerName;

		private Label _trophies;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_leaderboardUIEntry = _root.Q<VisualElement>("LeaderboardEntryParent").Required();
			_localPlayerHighlight = _root.Q<VisualElement>("LocalPlayerHighlight").Required();

			
			
			_rankNumber = _root.Q<Label>("RankNumber").Required();
			_playerName = _root.Q<Label>("PlayerName").Required();
			_trophies = _root.Q<Label>("TrophiesAmount").Required();
		}
		
		/// <summary>
		/// Sets the data needed to fill leaderboard entry's data
		/// </summary>
		/// <param name="data">All the match data from the player we're showing</param>
		/// <param name="isLocalPlayer">If this is the local player</param>
		public void SetData(PlayerLeaderboardEntry data, bool isLocalPlayer)
		{
			_leaderboardUIEntry.RemoveModifiers();
			
			if (data.Position <= 2)
			{
				var rankClass = data.Position switch
				{
					0 => UssLeaderboardEntryFirst,
					1 => UssLeaderboardEntrySecond,
					2 => UssLeaderboardEntryThird,
					_ => ""
				};
				
				_leaderboardUIEntry.AddToClassList(rankClass);
			}
			else
			{
				_rankNumber.text = $"{data.Position.ToString()}.";
			}

			if (isLocalPlayer)
			{
				_leaderboardUIEntry.AddToClassList(UssLeaderboardEntryLocal);
				_localPlayerHighlight.RemoveFromClassList(PlayerHighlightHidden);
				_localPlayerHighlight.AddToClassList(PlayerHighlight);
			}

			_playerName.text = data.DisplayName;

			_trophies.text = data.StatValue.ToString();
			var delayTime = 0.3f + data.Position * 0.1f;

			_leaderboardUIEntry.style.transitionDelay = new List<TimeValue>
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