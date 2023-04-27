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
	public class LeaderboardEntryView : IUIView
	{
		private const string USS_LEADERBOARD_ENTRY = "leaderboard-entry";
		private const string USS_LEADERBOARD_ENTRY_FIRST = USS_LEADERBOARD_ENTRY + "--first";
		private const string USS_LEADERBOARD_ENTRY_SECOND = USS_LEADERBOARD_ENTRY + "--second";
		private const string USS_LEADERBOARD_ENTRY_THIRD = USS_LEADERBOARD_ENTRY + "--third";
		private const string USS_LEADERBOARD_ENTRY_LOCAL = USS_LEADERBOARD_ENTRY + "--local";

		private const string USS_AVATAR_NFT = "leaderboard-entry__pfp--nft";

		private VisualElement _root;
		private VisualElement _leaderboardEntry;
		private Label _rankNumber;
		private Label _playerName;
		private Label _kills;
		private Label _trophies;
		private VisualElement _pfp;
		private VisualElement _pfpImage;

		private IGameServices _services;

		private int _pfpRequestHandle = -1;

		public void Attached(VisualElement element)
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_root = element;

			_leaderboardEntry = _root.Q<VisualElement>("LeaderboardEntryParent").Required();
			_rankNumber = _root.Q<Label>("RankNumber").Required();
			_playerName = _root.Q<Label>("PlayerName").Required();
			_kills = _root.Q<Label>("Kills").Required();
			_trophies = _root.Q<Label>("TrophiesAmount").Required();
			//_pfp = _root.Q<VisualElement>("PFP").Required();
			_pfp = element.Q("PFP").Required();
			_pfpImage = element.Q("PFPImage").Required();
		}

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
			_kills.text = playerKilledCount.ToString();
			_trophies.text = playerTrophies.ToString();

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
				_pfp.SetVisibility(false);
				_pfp.AddToClassList(USS_AVATAR_NFT);
				_pfpRequestHandle = _services.RemoteTextureService.RequestTexture(
					pfpUrl,
					tex =>
					{
						_pfpImage.style.backgroundImage = new StyleBackground(tex);
						_pfp.SetVisibility(true);
					},
					() =>
					{
						_pfp.RemoveFromClassList(USS_AVATAR_NFT);
						_pfp.SetVisibility(true);
					});
			}
			else
			{
				_pfpImage.style.backgroundImage = StyleKeyword.Null;
				_pfp.RemoveFromClassList(USS_AVATAR_NFT);
			}
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
			_services.RemoteTextureService.CancelRequest(_pfpRequestHandle);
		}
	}
}