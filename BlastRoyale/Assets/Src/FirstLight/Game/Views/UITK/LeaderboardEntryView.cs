using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using Newtonsoft.Json;
using Quantum;
using QuickEye.UIToolkit;
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
		[Q("MainContainer")] private VisualElement _leaderboardEntry;
		[Q("RankNumber")] private Label _rankNumber;
		[Q("PlayerName")] private Label _playerName;
		[Q("Kills")] private Label _insideMetric;
		[Q("TrophiesAmount")] private Label _mainMetric;
		[Q("PFP")] private VisualElement _pfp;
		[Q("PFPImage")] private VisualElement _pfpImage;
		[Q("TrophiesIcon")] private VisualElement _metricIcon;
		[Q("PfpFrameColor")] private VisualElement _border;
		[Q("ExtraOptions")] private ImageButton _extraOptions;

		private IGameServices _services;
		private ICollectionService _collectionService;

		private int _pfpRequestHandle = -1;

		protected override void Attached()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_collectionService = _services.CollectionService;
			_leaderboardEntry.RegisterCallback<MouseDownEvent>(OnClick);
			_extraOptions.SetVisibility(false);
		}

		public void SetIcon(string iconClass)
		{
			_metricIcon.ClearClassList();
			if (iconClass != null)
			{
				_metricIcon.AddToClassList($"{USS_LEADERBOARD_ENTRY}__{iconClass}");
			}
		}

		public void SetOptions(Action<VisualElement> onClick)
		{
			_extraOptions.SetVisibility(true);
			_extraOptions.clicked += () => onClick(_extraOptions);
		}

		private void OnClick(MouseDownEvent e)
		{
			if (_playerId == null) return;
			_services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(new PlayerStatisticsPopupPresenter.StateData()
			{
				PlayfabID = _playerId
			}).Forget();
		}

		/// <summary>
		/// Sets the data needed to fill leaderboard entry's data.
		/// </summary>
		public void SetData(int rank, string playerName, int playerKilledCount, int playerTrophies, bool isLocalPlayer, string playerId, Color? borderColor)
		{
			_leaderboardEntry.RemoveModifiers();

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
		}

		public void SetLeaderboardEntryPFPSprite(Sprite pfp)
		{
			if (pfp == null)
			{
				_pfpImage.style.backgroundImage = StyleKeyword.Null;
				return;
			}

			_pfpImage.style.backgroundImage = new StyleBackground(pfp);
		}

		public void SetLeaderboardEntryPFPUrl(string pfpUrl)
		{
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