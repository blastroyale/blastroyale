using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual components of the LeaderboardEntry elements in the LeaderboardAndRewardsScreen
	/// </summary>
	public class LeaderboardEntryView : UIView
	{
		private const string USS_LEADERBOARD_ENTRY = "leaderboard-entry";
		private const string USS_LEADERBOARD_ENTRY_FIRST = USS_LEADERBOARD_ENTRY + "--first";
		private const string USS_LEADERBOARD_ENTRY_SECOND = USS_LEADERBOARD_ENTRY + "--second";
		private const string USS_LEADERBOARD_ENTRY_THIRD = USS_LEADERBOARD_ENTRY + "--third";
		private const string USS_LEADERBOARD_ENTRY_LOCAL = USS_LEADERBOARD_ENTRY + "--local";

		private const string USS_AVATAR_NFT = "leaderboard-entry__pfp--nft";

		private VisualElement _leaderboardEntry;
		private Label _rankNumber;
		private Label _playerName;
		private Label _insideMetric;
		private Label _mainMetric;
		private VisualElement _pfp;
		private VisualElement _pfpImage;
		private VisualElement _metricIcon;

		private IGameServices _services;

		private int _pfpRequestHandle = -1;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_services = MainInstaller.Resolve<IGameServices>();

			_leaderboardEntry = element.Q<VisualElement>("LeaderboardEntryParent").Required();
			_rankNumber = element.Q<Label>("RankNumber").Required();
			_playerName = element.Q<Label>("PlayerName").Required();
			_insideMetric = element.Q<Label>("Kills").Required();
			_mainMetric = element.Q<Label>("TrophiesAmount").Required();
			_pfp = element.Q("PFP").Required();
			_pfpImage = element.Q("PFPImage").Required();
			_metricIcon = element.Q("TrophiesIcon").Required();
		}

		public void SetIcon(string iconClass)
		{
			_metricIcon.ClearClassList();
			if (iconClass != null)
			{
				_metricIcon.AddToClassList($"{USS_LEADERBOARD_ENTRY}__{iconClass}");
			}
		}
		
		public VisualElement MetricIcon => _metricIcon;
		
		/// <summary>
		/// Sets the data needed to fill leaderboard entry's data.
		/// </summary>
		public void SetData(int rank, string playerName, int playerKilledCount, int playerTrophies, bool isLocalPlayer,
							string pfpUrl)
		{
			_leaderboardEntry.RemoveModifiers();

			if (rank <= 3)
			{
				var rankClass = rank switch
				{
					1 => USS_LEADERBOARD_ENTRY_FIRST,
					2 => USS_LEADERBOARD_ENTRY_SECOND,
					3 => USS_LEADERBOARD_ENTRY_THIRD,
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
				_leaderboardEntry.AddToClassList(USS_LEADERBOARD_ENTRY_LOCAL);
			}

			_playerName.text = playerName;
			_insideMetric.text = playerKilledCount.ToString();
			_mainMetric.text = playerTrophies.ToString();

			var delayTime = 0.3f + rank * 0.1f;

			_leaderboardEntry.style.transitionDelay = new List<TimeValue>
			{
				delayTime, delayTime
			};

			// pfpUrl = "https://mainnetprodflghubstorage.blob.core.windows.net/collections/corpos/1.png".Replace("1.png",
			// 	$"{Random.Range(1, 888)}.png");

			// PFP
			if (!string.IsNullOrEmpty(pfpUrl))
			{
				_pfp.AddToClassList(USS_AVATAR_NFT);
				_pfpRequestHandle = _services.RemoteTextureService.RequestTexture(
					pfpUrl,
					tex =>
					{
						_pfpImage.style.backgroundImage = new StyleBackground(tex);
					},
					() =>
					{
						_pfp.RemoveFromClassList(USS_AVATAR_NFT);
					});
			}
			else
			{
				_pfpImage.style.backgroundImage = StyleKeyword.Null;
				_pfp.RemoveFromClassList(USS_AVATAR_NFT);
			}
		}

		public override void UnsubscribeFromEvents()
		{
			_services.RemoteTextureService.CancelRequest(_pfpRequestHandle);
		}
	}
}