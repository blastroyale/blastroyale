using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display the current leading player and how many frags they have.
	/// </summary>
	public class LeaderHolderView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _leaderNameText;
		[SerializeField, Required] private TextMeshProUGUI _currentLeaderFragsText;
		[SerializeField, Required] private Animation _leaderChangedAnimation;

		private PlayerRef? _currentLeader;
		
		private void Awake()
		{
			_currentLeaderFragsText.text = "0";
			_leaderNameText.text = "";
			_currentLeader = null;
			
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
		}

		/// <summary>
		/// The scoreboard could update whilst it's open, e.g. players killed whilst looking at it, etc.
		/// </summary>
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var leaderData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerLeader));

			if (!_currentLeader.HasValue || _currentLeader.Value != callback.PlayerLeader)
			{
				_currentLeader = callback.PlayerLeader;
				_leaderNameText.text = leaderData.GetPlayerName();
				
				_leaderChangedAnimation.Rewind();
				_leaderChangedAnimation.Play();
			}
			
			_currentLeaderFragsText.text = leaderData.Data.PlayersKilledCount.ToString();
		}
	}
}
