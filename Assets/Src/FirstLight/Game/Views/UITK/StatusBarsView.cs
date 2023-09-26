using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles displaying the player bars on the screen.
	/// </summary>
	public class StatusBarsView : UIView
	{
		private Camera _camera;

		private IMatchServices _matchServices;
		private IGameServices _gameServices;

		private readonly Dictionary<EntityRef, Transform> _anchors = new();
		private readonly Dictionary<EntityRef, PlayerStatusBarElement> _visiblePlayers = new();
		private readonly HashSet<EntityRef> _culledPlayers = new();
		private readonly List<EntityRef> _entityCache = new(5);
		private readonly StyleColor _defaultShieldDmgColor = new StyleColor(new Color(0.2f, 0.72f, 1f));

		// TODO: Only returned to pool when it's destroyed, and they're not culled
		private readonly Dictionary<EntityRef, HealthStatusBarElement> _healthBars = new();

		private ObjectPool<HealthStatusBarElement> _healthBarPool;
		private ObjectPool<PlayerStatusBarElement> _playerBarPool;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_camera = FLGCamera.Instance.MainCamera;
			_matchServices = MainInstaller.ResolveMatchServices();
			_gameServices = MainInstaller.ResolveServices();

			element.Clear();

			_playerBarPool = new ObjectPool<PlayerStatusBarElement>(
				() =>
				{
					var bar = new PlayerStatusBarElement();
					bar.SetDisplay(false);
					Element.Add(bar);
					return bar;
				},
				pbe => pbe.SetDisplay(true),
				pbe => pbe.SetDisplay(false),
				pbe => pbe.RemoveFromHierarchy(),
				false, 3);

			_healthBarPool = new ObjectPool<HealthStatusBarElement>(
				() =>
				{
					var bar = new HealthStatusBarElement();
					bar.SetDisplay(false);
					Element.Add(bar);
					return bar;
				},
				pbe => pbe.SetDisplay(true),
				pbe => pbe.SetDisplay(false),
				pbe => pbe.RemoveFromHierarchy(),
				false, 3);
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLand);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnHealthChanged>(this, OnHealthChanged);
			QuantumEvent.SubscribeManual<EventOnShieldChanged>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerLevelUp>(this, OnPlayerLevelUp);
			QuantumEvent.SubscribeManual<EventOnPlayerAmmoChanged>(this, OnPlayerAmmoChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.SubscribeManual<EventOnShrinkingCircleDmg>(this, OnShrinkingCircleDmg);
			QuantumEvent.SubscribeManual<EventOnCollectableBlocked>(this, OnCollectableBlocked);
			QuantumEvent.SubscribeManual<EventOnPlayerReloadStart>(this, OnPlayerReloadStart);
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnUpdateView);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);

			_gameServices.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnUpdateView(CallbackUpdateView callback)
		{
			var f = QuantumRunner.Default.Game.Frames.Predicted;

			// Un-Cull players that are currently culled but they shouldn't be
			_entityCache.Clear();
			foreach (var player in _culledPlayers)
			{
				if (f.IsCulled(player)) continue;

				InitBar(f, player);
				_entityCache.Add(player);
			}

			foreach (var entity in _entityCache)
			{
				_culledPlayers.Remove(entity);
			}

			// Cull players that are currently not culled but should be
			_entityCache.Clear();
			foreach (var (player, value) in _visiblePlayers)
			{
				if (!f.IsCulled(player)) continue;

				_culledPlayers.Add(player);
				_playerBarPool.Release(value);

				_entityCache.Add(player);
			}

			foreach (var entity in _entityCache)
			{
				_visiblePlayers.Remove(entity);
			}

			Assert.AreEqual(_visiblePlayers.Count, _playerBarPool.CountActive, "Player bar pool mismatch!");

			// Update all position of UI elements of all the un-culled players.
			foreach (var (entity, bar) in _visiblePlayers)
			{
				var anchor = _anchors[entity];
				if (anchor == null) continue;
				var screenPoint = _camera.WorldToScreenPoint(anchor.position);
				screenPoint.y = _camera.pixelHeight - screenPoint.y;

				var panelPos = RuntimePanelUtils.ScreenToPanel(Element.panel, screenPoint);
				bar.transform.position = panelPos;
			}

			// Duplicates above ^
			foreach (var (entity, bar) in _healthBars)
			{
				// TODO: https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/issue/915
				if (!_anchors.TryGetValue(entity, out var anchor))
				{
					FLog.Warn($"Failed to restore anchor for entity {entity}, likely due to reconnection, skipping");
					continue;
				}
				if (anchor == null) continue;
				var screenPoint = _camera.WorldToScreenPoint(anchor.position);
				screenPoint.y = _camera.pixelHeight - screenPoint.y;

				var panelPos = RuntimePanelUtils.ScreenToPanel(Element.panel, screenPoint);
				bar.transform.position = panelPos;
			}
		}

		public void InitAll()
		{
			var f = QuantumRunner.Default.Game.Frames.Verified;
			var dataArray = f.GetSingleton<GameContainer>().PlayersData;
			for (int i = 0; i < f.PlayerCount; i++)
			{
				InitPlayer(f, dataArray[i].Entity);
			}
		}
		
		private void OnPlayerSkydiveLand(EventOnPlayerSkydiveLand callback)
		{
			InitPlayer(callback.Game.Frames.Predicted, callback.Entity);
		}

		private void InitPlayer(Frame f, EntityRef entity)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(entity, out var view)) return;

			// TODO: https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/issue/915
			if (_anchors.ContainsKey(entity))
			{
				FLog.Warn("Unhandled reconnection flow initializing entity twice, ignoring it for now");
				return;
			}
			_anchors.Add(entity, view.GetComponent<HealthEntityBase>().HealthBarAnchor);

			if (f.IsCulled(entity))
			{
				_culledPlayers.Add(entity);
				return;
			}

			InitBar(f, entity);
		}

		private unsafe void InitBar(Frame f, EntityRef entity)
		{
			var bar = _playerBarPool.Get();
			_visiblePlayers.Add(entity, bar);

			var pc = f.Get<PlayerCharacter>(entity);
			var pd = f.GetPlayerData(pc.Player);
			var stats = f.Get<Stats>(entity);
			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			var isFriendlyPlayer = (spectatedPlayer.Entity == entity || pc.TeamId > 0 && pc.TeamId == spectatedPlayer.Team);
			var hidePlayerNames = f.Context.TryGetMutatorByType(MutatorType.HidePlayerNames, out _) && !isFriendlyPlayer;
			var playerName = hidePlayerNames ? string.Empty : Extensions.GetPlayerName(f, entity, pc);
			var nameColor = pd != null
				                ? _gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) pd.LeaderboardRank)
				                : GameConstants.PlayerName.DEFAULT_COLOR;

			bar.SetName(playerName, nameColor);
			bar.SetIsFriendly(isFriendlyPlayer);
			bar.SetLevel(pc.GetEnergyLevel(f));
			bar.SetHealth(stats.CurrentHealth, stats.CurrentHealth,
				stats.Values[(int) StatType.Health].StatValue.AsInt);
			bar.SetShield(stats.CurrentShield, stats.Values[(int) StatType.Shield].StatValue.AsInt);
			bar.SetMagazine(pc.WeaponSlot->MagazineShotCount, pc.WeaponSlot->MagazineSize);
			bar.SetIconColor(nameColor);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			_culledPlayers.Remove(callback.Entity);

			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			_visiblePlayers.Remove(callback.Entity);
			_playerBarPool.Release(bar);
		}

		private void OnPlayerLevelUp(EventOnPlayerLevelUp callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.SetLevel(callback.CurrentLevel);

			if (_matchServices.IsSpectatingPlayer(callback.Entity))
			{
				bar.ShowNotification(PlayerStatusBarElement.NotificationType.LevelUp);
			}
		}

		private void OnShieldChanged(EventOnShieldChanged callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.SetShield(callback.CurrentShield, callback.CurrentShieldCapacity);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.SetHealth(callback.PreviousHealth, callback.CurrentHealth, callback.MaxHealth);
		}

		private void OnPlayerAmmoChanged(EventOnPlayerAmmoChanged callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.SetMagazine(callback.CurrentMag, callback.MaxMag);
		}

		private void OnCollectableBlocked(EventOnCollectableBlocked callback)
		{
			if (!_matchServices.IsSpectatingPlayer(callback.PlayerEntity)) return;
			if (!callback.Game.Frames.Verified.TryGet<Consumable>(callback.CollectableEntity, out var consumable))
				return;
			if (!_visiblePlayers.TryGetValue(callback.PlayerEntity, out var bar)) return;

			switch (consumable.ConsumableType)
			{
				case ConsumableType.Health:
					bar.ShowNotification(PlayerStatusBarElement.NotificationType.MaxHealth);
					break;
				case ConsumableType.Shield:
					bar.ShowNotification(PlayerStatusBarElement.NotificationType.MaxShields);
					break;
				case ConsumableType.Ammo:
					bar.ShowNotification(PlayerStatusBarElement.NotificationType.MaxAmmo);
					break;
				default:
					FLog.Error($"Unknown collectable: {callback.CollectableId}");
					break;
			}
		}

		private unsafe void OnPlayerReloadStart(EventOnPlayerReloadStart callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;
			if (!callback.Game.Frames.Verified.TryGet<PlayerCharacter>(callback.Entity, out var pc)) return;

			bar.ShowReload((int) (pc.WeaponSlot->ReloadTime.AsFloat * 1000));
		}

		private unsafe void OnPlayerAttackHit(EventOnPlayerAttackHit callback)
		{
			var f = callback.Game.Frames.Verified;

			if (f.Has<Destructible>(callback.HitEntity) &&
				f.Unsafe.TryGetPointer<Stats>(callback.HitEntity, out var stats))
			{
				if (!_healthBars.TryGetValue(callback.HitEntity, out var bar))
				{
					bar = _healthBars[callback.HitEntity] = _healthBarPool.Get();
			
					_anchors[callback.HitEntity] = _matchServices.EntityViewUpdaterService
						.GetManualView(callback.HitEntity).GetComponent<HealthEntityBase>().HealthBarAnchor;
				}

				bar.SetHealth((float) stats->CurrentHealth / stats->Values[(int) StatType.Health].StatValue.AsInt);

				return;
			}
			else if (_healthBars.TryGetValue(callback.HitEntity, out var bar))
			{
				// Destructible destroyed
				bar.SetHealth(0f);
				_healthBarPool.Release(bar);
			}

			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			if ((callback.PlayerTeamId == spectatedPlayer.Team || callback.HitEntity == spectatedPlayer.Entity) &&
				_visiblePlayers.TryGetValue(callback.HitEntity, out var playerBar))
			{
				playerBar.PingDamage(callback.TotalDamage, callback.isShieldDmg ? _defaultShieldDmgColor : null);
			}
		}

		private unsafe void OnShrinkingCircleDmg(EventOnShrinkingCircleDmg callback)
		{
			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			if (callback.HitEntity == spectatedPlayer.Entity && _visiblePlayers.TryGetValue(callback.HitEntity, out var playerBar))
			{
				playerBar.PingDamage(callback.TotalDamage);
			}
		}
	}
}