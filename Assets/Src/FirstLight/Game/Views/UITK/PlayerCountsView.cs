using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class PlayerCountsView : UIView
	{
		private Label _aliveCountLabel;
		private VisualElement _aliveCountPing;
		private Label _killsCountLabel;
		private VisualElement _killsCountPing;
		private Label _teamsCountLabel;
		private VisualElement _teamsCountPing;

		private IGameServices _gameServices;
		private IMatchServices _matchServices;

		private int _killsCount = -1;
		private int _teamsCount = -1;

		private bool _showTeamCount = true;

		private readonly HashSet<int> _teamsCache = new ();

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_gameServices = MainInstaller.ResolveServices();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_aliveCountLabel = element.Q<Label>("AliveCountText").Required();
			_aliveCountPing = element.Q<VisualElement>("AliveCountPing").Required();
			_killsCountLabel = element.Q<Label>("KilledCountText").Required();
			_killsCountPing = element.Q<VisualElement>("KilledCountPing").Required();
			_showTeamCount = _gameServices.RoomService.CurrentRoom.GameModeConfig.ShowTeamCount;
			if (_showTeamCount)
			{
				_teamsCountLabel = element.Q<Label>("TeamsCountText").Required();
				_teamsCountPing = element.Q<VisualElement>("TeamsCountPing").Required();
			}
			else
			{
				element.Q("TeamsContainer").Required().SetDisplay(false);
			}
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			_matchServices.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			UpdatePlayerCounts(callback.Game.Frames.Verified);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer current)
		{
			if (!current.Entity.IsValid) return;

			UpdatePlayerCounts(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void UpdatePlayerCounts(Frame f)
		{
			var container = f.GetSingleton<GameContainer>();

			// We count all alive players in a match and display this number
			var playersAlive = 0;
			_teamsCache.Clear();
			for (int i = 0; i < container.PlayersData.Length; i++)
			{
				var data = container.PlayersData[i];

				if (data.IsValid && data.Entity.IsAlive(f))
				{
					_teamsCache.Add(data.TeamId);
					playersAlive++;
				}
			}

			_aliveCountLabel.text = playersAlive.ToString();
			_aliveCountLabel.AnimatePing();
			_aliveCountPing.AnimatePingOpacity();

			if (_showTeamCount && _teamsCount != _teamsCache.Count)
			{
				_teamsCount = _teamsCache.Count;
				_teamsCountLabel.text = _teamsCount.ToString();
				_teamsCountLabel.AnimatePing();
				_teamsCountPing.AnimatePingOpacity();
			}

			// Check tries to fix https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/issue/2681
			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			var killsCount = spectatedPlayer.Player.IsValid
				? container.PlayersData[_matchServices.SpectateService.SpectatedPlayer.Value.Player].PlayersKilledCount
				: 0;
			if (killsCount != _killsCount)
			{
				_killsCount = (int) killsCount;
				_killsCountLabel.text = killsCount.ToString();
				_killsCountLabel.AnimatePing();
				_killsCountPing.AnimatePingOpacity();
			}
		}
	}
}