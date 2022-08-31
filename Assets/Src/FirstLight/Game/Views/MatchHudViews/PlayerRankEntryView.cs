using FirstLight.Game.Logic;
using I2.Loc;
using Photon.Realtime;
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
		
		/// <summary>
		/// Set the information of this player entry based on the given leaderboard entry
		/// </summary>
		public void SetInfo()
		{

		}
	}
}