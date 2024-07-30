using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Photon.Deterministic;
using Quantum;
using Quantum.Systems;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class LocalPlayerInfoView : UIView
	{
		private PlayerHealthShieldElement _healthShield;
		private InGamePlayerAvatar _pfp;
		private Label _name;

		private IGameServices _gameServices;
		private IMatchServices _matchServices;
		private IGameDataProvider _dataProvider;

		protected override void Attached()
		{
			_gameServices = MainInstaller.ResolveServices();
			_matchServices = MainInstaller.ResolveMatchServices();
			_dataProvider = MainInstaller.ResolveData();

			_healthShield = Element.Q<PlayerHealthShieldElement>("LocalPlayerHealthShield").Required();
			_pfp = Element.Q<InGamePlayerAvatar>("Avatar").Required();
			_name = Element.Q<Label>("LocalPlayerName").Required();
		}

		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnHealthChangedVerified>(this, OnHealthChangedVerified);
			QuantumEvent.SubscribeManual<EventOnShieldChangedPredicted>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnTeamAssigned>(this, OnTeamAssigned);
		}

		public override void OnScreenClose()
		{
			base.OnScreenClose();
			QuantumEvent.UnsubscribeListener(this);
		}

		public void UpdateFromLatestVerifiedFrame()
		{
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();
			var f = QuantumRunner.Default.Game.Frames.Verified;

			UpdateHealthAndShieldFromFrame(f);

			if (f.TryGet<PlayerCharacter>(playerEntity, out var pc))
			{
				var isBot = f.Has<BotCharacter>(playerEntity);
				var playerName = Extensions.GetPlayerName(f, playerEntity, pc);
				var playerNameColor = isBot
					? GameConstants.PlayerName.DEFAULT_COLOR
					: _gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked,
						(int) f.GetPlayerData(pc.Player).LeaderboardRank);

				_name.text = playerName;
				_name.style.color = playerNameColor;
			}

			UpdateTeamColor();
			_ = LoadPFP();
		}

		private void UpdateHealthAndShieldFromFrame(Frame f)
		{
			var playerEntity = _matchServices.SpectateService.GetSpectatedEntity();
			if (f.TryGet<Stats>(playerEntity, out var stats))
			{
				var maxHealth = FPMath.RoundToInt(stats.GetStatData(StatType.Health).StatValue);
				var maxShield = FPMath.RoundToInt(stats.GetStatData(StatType.Shield).StatValue);

				_healthShield.UpdateHealth(stats.CurrentHealth, stats.CurrentHealth, maxHealth);
				_healthShield.UpdateShield(stats.CurrentShield, stats.CurrentShield, maxShield);
			}
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			UpdateFromLatestVerifiedFrame();
		}

		private void OnTeamAssigned(EventOnTeamAssigned callback)
		{
			UpdateTeamColor();
		}

		private void OnShieldChanged(EventOnShieldChangedPredicted callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			_healthShield.UpdateShield(callback.PreviousValue, callback.CurrentValue, callback.CurrentMax);
		}

		private void OnHealthChangedVerified(EventOnHealthChangedVerified callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			_healthShield.UpdateHealth(callback.PreviousValue, callback.CurrentValue, callback.CurrentMax);
		}

		private void UpdateTeamColor()
		{
			_pfp.RemoveBorder();
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();

			if (TeamSystem.GetTeamMemberEntities(QuantumRunner.Default.VerifiedFrame(), playerEntity).Length < 1)
			{
				return;
			}

			var teamColor = _gameServices.TeamService.GetTeamMemberColor(playerEntity);
			if (teamColor.HasValue)
			{
				_pfp.SetTeamColor(teamColor);
			}
		}

		private async UniTask LoadPFP()
		{
			var itemData = _dataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PLAYER_SKINS);
			var sprite = await _gameServices.CollectionService.LoadCollectionItemSprite(itemData);
			_pfp.SetSprite(sprite);
		}
	}
}