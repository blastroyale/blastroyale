using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Photon.Deterministic;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class LocalPlayerInfoView : UIView
	{
		private PlayerHealthShieldElement _healthShield;
		private VisualElement _teamColor;
		private VisualElement _pfp;

		private IMatchServices _matchServices;
		private IGameDataProvider _dataProvider;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_matchServices = MainInstaller.ResolveMatchServices();
			_dataProvider = MainInstaller.ResolveData();

			_healthShield = element.Q<PlayerHealthShieldElement>("LocalPlayerHealthShield").Required();
			_teamColor = element.Q("TeamColor").Required();
			_pfp = element.Q("PlayerAvatar").Required();
		}

		public override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			QuantumEvent.SubscribeManual<EventOnHealthChanged>(this, OnHealthChanged);
			QuantumEvent.SubscribeManual<EventOnShieldChanged>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnTeamAssigned>(this, OnTeamAssigned);
		}

		public override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			QuantumEvent.UnsubscribeListener(this);
		}

		public void UpdateFromLatestVerifiedFrame()
		{
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();
			var f = QuantumRunner.Default.Game.Frames.Verified;

			if (f.TryGet<Stats>(playerEntity, out var stats))
			{
				var maxHealth = FPMath.RoundToInt(stats.GetStatData(StatType.Health).StatValue);
				var maxShield = FPMath.RoundToInt(stats.GetStatData(StatType.Shield).StatValue);

				_healthShield.UpdateHealth(stats.CurrentHealth, stats.CurrentHealth, maxHealth, !_dataProvider.AppDataProvider.ShowRealDamage);
				_healthShield.UpdateShield(stats.CurrentShield, stats.CurrentShield, maxShield, !_dataProvider.AppDataProvider.ShowRealDamage);
			}

			UpdateTeamColor();
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
			_healthShield.UpdateShield(callback.PreviousShield, callback.CurrentShield, callback.CurrentShieldCapacity,
				!_dataProvider.AppDataProvider.ShowRealDamage);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.Entity)) return;
			_healthShield.UpdateHealth(callback.PreviousHealth, callback.CurrentHealth, callback.MaxHealth,
				!_dataProvider.AppDataProvider.ShowRealDamage);
		}

		private void UpdateTeamColor()
		{
			var playerEntity = QuantumRunner.Default.Game.GetLocalPlayerEntityRef();

			if (TeamHelpers.GetTeamMembers(QuantumRunner.Default.PredictedFrame(), playerEntity).Count < 1)
			{
				_teamColor.SetVisibility(false);
				return;
			}

			var teamColor = _matchServices.TeamService.GetTeamMemberColor(playerEntity);
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
	}
}