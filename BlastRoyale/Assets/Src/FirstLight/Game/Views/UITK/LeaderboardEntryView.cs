using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using Newtonsoft.Json;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

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

		private string _playerId;
		private VisualElement _leaderboardEntry;
		private Label _rankNumber;
		private Label _playerName;
		private Label _insideMetric;
		private Label _mainMetric;
		private VisualElement _pfp;
		private VisualElement _pfpImage;
		private VisualElement _metricIcon;
		private VisualElement _border;

		private IGameServices _services;

		private int _pfpRequestHandle = -1;

		protected override void Attached()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_leaderboardEntry = Element.Q<VisualElement>("LeaderboardEntryParent").Required();
			_leaderboardEntry.RegisterCallback<MouseDownEvent>(OnClick);
			_rankNumber = Element.Q<Label>("RankNumber").Required();
			_playerName = Element.Q<Label>("PlayerName").Required();
			_insideMetric = Element.Q<Label>("Kills").Required();
			_mainMetric = Element.Q<Label>("TrophiesAmount").Required();
			_pfp = Element.Q("PFP").Required();
			_pfpImage = Element.Q("PFPImage").Required();
			_metricIcon = Element.Q("TrophiesIcon").Required();
			_border = Element.Q("PfpFrameColor").Required();
		}

		public void SetIcon(string iconClass)
		{
			_metricIcon.ClearClassList();
			if (iconClass != null)
			{
				_metricIcon.AddToClassList($"{USS_LEADERBOARD_ENTRY}__{iconClass}");
			}
		}

		private void OnClick(MouseDownEvent e)
		{
			if (_playerId == null) return;
			_services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(new PlayerStatisticsPopupPresenter.StateData()
			{
				PlayfabID = _playerId
			}).Forget();
		}
		
		public VisualElement MetricIcon => _metricIcon;
		
		/// <summary>
		/// Sets the data needed to fill leaderboard entry's data.
		/// </summary>
		public void SetData(int rank, string playerName, int playerKilledCount, int playerTrophies, bool isLocalPlayer,
							string pfpUrl, string playerId, Color? borderColor)
		{
			_leaderboardEntry.RemoveModifiers();
			
			/*
			if (borderColor.HasValue && borderColor.Value != GameConstants.PlayerName.DEFAULT_COLOR)//  rank > 0 && rank <= GameConstants.Data.LEADERBOARD_BRONZE_ENTRIES)
			{
				_border.SetDisplay(true);
				_border.style.unityBackgroundImageTintColor = borderColor.Value;
			}
			else
			{
				_border.SetDisplay(false);
			}
			*/
			
			_playerName.style.color = borderColor.Value;
			_rankNumber.style.color = borderColor.Value;
			
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
				_pfpRequestHandle = _services.RemoteTextureService.RequestTexture(
					pfpUrl,
					tex =>
					{
						_pfpImage.style.backgroundImage = new StyleBackground(tex);
						_pfpImage.style.top = -tex.height / 4;
					},
					() =>
					{
					});
			}
			else
			{
				_pfpImage.style.backgroundImage = StyleKeyword.Null;
			}
		}

		public override void OnScreenClose()
		{
			_services.RemoteTextureService.CancelRequest(_pfpRequestHandle);
		}
	}
}