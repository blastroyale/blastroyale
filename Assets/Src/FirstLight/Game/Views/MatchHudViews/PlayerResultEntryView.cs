using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class shows match data information for all players at the end of the match, e.g. total kills, deaths, ranking, etc.
	/// </summary>
	public class PlayerResultEntryView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _playerNameText;
		[SerializeField, Required] private TextMeshProUGUI _playerRankText;
		[SerializeField, Required] private TextMeshProUGUI _killsText;
		[SerializeField, Required] private TextMeshProUGUI _deathsText;
		[SerializeField, Required] private TextMeshProUGUI _xpText;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		
		/// <summary>
		/// Set the information of this player entry ranking based on the given <paramref name="data"/> & <paramref name="awards"/>
		/// </summary>
		public void SetInfo(QuantumPlayerMatchData data, bool showExtra = true)
		{
			_dataProvider ??= MainInstaller.Resolve<IGameDataProvider>();
			_services ??= MainInstaller.Resolve<IGameServices>();

			var rewards = _dataProvider.RewardDataProvider.UnclaimedRewards;
			var col = data.IsLocalPlayer ? Color.yellow : Color.white;

			_playerNameText.text = data.GetPlayerName();
			_playerRankText.text = $"{data.PlayerRank.ToString()}.";
			_killsText.text = data.Data.PlayersKilledCount.ToString();
			_deathsText.text = data.Data.DeathCount.ToString();
			
			_xpText.enabled = showExtra;
			_xpText.text = data.Data.PlayerTrophies.ToString();

			_playerNameText.color = col;
			_playerRankText.color = col;
			_killsText.color = col;
			_deathsText.color = col;
		}
	}
}