using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using PlayFab.ClientModels;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual components of the LeaderboardEntry elements in the LeaderboardAndRewardsScreen
	/// </summary>
	public class LeaderboardUIEntryView : IUIView
	{
		private const string USS_LEADERBOARD_ENTRY_FIRST = "leaderboard-entry--first";
		private const string USS_LEADERBOARD_ENTRY_SECOND = "leaderboard-entry--second";
		private const string USS_LEADERBOARD_ENTRY_THIRD = "leaderboard-entry--third";
		private const string USS_LEADERBOARD_ENTRY_LOCAL = "leaderboard-entry--local";

		public float AnimStartDelayTime = 0.3f;
		public float AnimItemOffsetTime = 0.1f;

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
		public void SetData(PlayerLeaderboardEntry data, bool isLocalPlayer, int posInLeaderboardContainer)
		{
			_leaderboardUIEntry.RemoveModifiers();
			
			if (data.Position <= 2)
			{
				var rankClass = data.Position switch
				{
					0 => USS_LEADERBOARD_ENTRY_FIRST,
					1 => USS_LEADERBOARD_ENTRY_SECOND,
					2 => USS_LEADERBOARD_ENTRY_THIRD,
					_ => ""
				};
				
				_leaderboardUIEntry.AddToClassList(rankClass);
			}
			else
			{
				_rankNumber.text = $"{(data.Position+1).ToString()}.";
			}

			if (isLocalPlayer)
			{
				_leaderboardUIEntry.AddToClassList(USS_LEADERBOARD_ENTRY_LOCAL);
			}
			else
			{
				_localPlayerHighlight.SetVisibility(false);
				_localPlayerHighlight.SetDisplay(false);
			}

			_playerName.text = data.DisplayName.Remove(data.DisplayName.Length-5);

			_trophies.text = data.StatValue.ToString();
			
			var delayTime = AnimStartDelayTime + posInLeaderboardContainer * AnimItemOffsetTime;

			_leaderboardUIEntry.style.transitionDelay = new List<TimeValue>
			{
				delayTime, delayTime
			};
		}

		private void SetAnimDelay(){}
		public void SubscribeToEvents()
		{
			
		}

		public void UnsubscribeFromEvents()
		{
			
		}

	
	}
}