using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class shows match data information for all players at the end of the match, e.g. total kills, deaths, ranking, etc.
	/// </summary>
	public class PlayerResultEntryView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private TextMeshProUGUI _playerRankText;
		[SerializeField] private TextMeshProUGUI _killsText;
		[SerializeField] private TextMeshProUGUI _deathsText;
		[SerializeField] private TextMeshProUGUI _xpText;
		[SerializeField] private TextMeshProUGUI _coinsText;

		private IGameDataProvider _dataProvider;
		
		/// <summary>
		/// Set the information of this player entry ranking based on the given <paramref name="data"/> & <paramref name="awards"/>
		/// </summary>
		public void SetInfo(QuantumPlayerMatchData data, bool showExtra = true)
		{
			_dataProvider ??= MainInstaller.Resolve<IGameDataProvider>();

			var rewards = _dataProvider.RewardDataProvider.GetMatchRewards(data, false);
			var col = data.IsLocalPlayer ? Color.yellow : Color.white;
			
			_playerNameText.text = data.GetPlayerName();
			_playerRankText.text = $"{data.PlayerRank.ToString()}.";
			_killsText.text = data.Data.PlayersKilledCount.ToString();
			_deathsText.text = data.Data.DeathCount.ToString();
			_coinsText.text = rewards.TryGetValue(GameId.HC, out var cs) ? cs.ToString() : "0";
			_coinsText.enabled = showExtra;
			
			_playerNameText.color = col;
			_playerRankText.color = col;
			_killsText.color = col;
			_deathsText.color = col;
			_coinsText.color = col;
		}
	}
}