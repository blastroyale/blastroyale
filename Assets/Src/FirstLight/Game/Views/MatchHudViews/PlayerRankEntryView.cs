using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class shows player ranking inside of leaderboard on LeaderboardScreenPresenter
	/// </summary>
	public class PlayerRankEntryView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _rankText;
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private TextMeshProUGUI _trophiesText;
		[SerializeField] private TextMeshProUGUI _rewardAmountText;
		[SerializeField] private Image _rewardIcon;
		[SerializeField] private Image _backgroundImage;
		[SerializeField] private Color _topRanksColor;
		[SerializeField] private Color _rewardedRanksColor;
		[SerializeField] private Color _nonRewardedRanksColor;
		[SerializeField] private Color _youTextColor;
		
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
		
		/// <summary>
		/// Set the information of this player entry based on the given leaderboard entry
		/// </summary>
		public async void SetInfo(int rank, string playerName, int trophies, bool localPlayer, Tuple<GameId,int> rewardAndAmount)
		{
			_rankText.text = $"#{rank}";
			_playerNameText.text = playerName.TrimPlayerNameNumbers();
			_trophiesText.text = trophies.ToString();

			if (rewardAndAmount != null)
			{
				_rewardIcon.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(rewardAndAmount.Item1);
				_rewardAmountText.text = rewardAndAmount.Item2.ToString();
			}
			else
			{
				_rewardIcon.gameObject.SetActive(false);
				_rewardAmountText.text = "-";
			}

			if (localPlayer)
			{
				_rankText.color = _youTextColor;
				_playerNameText.color = _youTextColor;
				_trophiesText.color = _youTextColor;
				_rewardAmountText.color = _youTextColor;
			}
		}
	}
}