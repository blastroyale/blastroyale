using System.Collections.Generic;
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
		private VisualElement _teamColor;
		private VisualElement _pfp;
		private Label _name;

		private IGameServices _gameServices;
		private IMatchServices _matchServices;
		private IGameDataProvider _dataProvider;
		private HashSet<EventKey> _localPlayerEvents = new ();

		protected override void Attached()
		{
			_gameServices = MainInstaller.ResolveServices();
			_matchServices = MainInstaller.ResolveMatchServices();
			_dataProvider = MainInstaller.ResolveData();

			_healthShield = Element.Q<PlayerHealthShieldElement>("LocalPlayerHealthShield").Required();
			_teamColor = Element.Q("TeamColor").Required();
			_pfp = Element.Q("PlayerAvatar").Required();
			_name = Element.Q<Label>("LocalPlayerName").Required();
		}

		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnHealthChanged>(this, OnHealthChanged);
			QuantumEvent.SubscribeManual<EventOnShieldChanged>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnTeamAssigned>(this, OnTeamAssigned);
			QuantumCallback.SubscribeManual<CallbackEventCanceled>(this, OnEventCanceled);
			QuantumCallback.SubscribeManual<CallbackEventConfirmed>(this, OnEventConfirmed);
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

		private void OnShieldChanged(EventOnShieldChanged callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			_healthShield.UpdateShield(callback.PreviousShield, callback.CurrentShield, callback.CurrentShieldCapacity);
			_localPlayerEvents.Add(callback);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			_healthShield.UpdateHealth(callback.PreviousHealth, callback.CurrentHealth, callback.MaxHealth);
			_localPlayerEvents.Add(callback);
		}

		private void UpdateTeamColor()
		{
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();

			if (TeamSystem.GetTeamMembers(QuantumRunner.Default.PredictedFrame(), playerEntity).Count < 1)
			{
				_teamColor.SetVisibility(false);
				return;
			}

			var teamColor = _gameServices.TeamService.GetTeamMemberColor(playerEntity);
			if (teamColor.HasValue)
			{
				_teamColor.SetVisibility(true);
				_teamColor.style.backgroundColor = teamColor.Value;
			}
			else
			{
				_teamColor.SetVisibility(false);
			}
		}

		private async UniTask LoadPFP()
		{
			var itemData = _dataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PROFILE_PICTURE);
			var sprite = await _gameServices.CollectionService.LoadCollectionItemSprite(itemData);
			_pfp.style.backgroundImage = new StyleBackground(sprite);
		}


		private void OnEventCanceled(CallbackEventCanceled callback)
		{
			if (!_localPlayerEvents.Contains(callback.EventKey)) return;

			UpdateHealthAndShieldFromFrame(callback.Game.Frames.Verified);
			_localPlayerEvents.Remove(callback.EventKey);
		}

		private void OnEventConfirmed(CallbackEventConfirmed callback)
		{
			_localPlayerEvents.Remove(callback.EventKey);
		}
	}
}