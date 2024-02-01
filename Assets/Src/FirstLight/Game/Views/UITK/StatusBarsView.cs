using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
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
		private const bool SHOW_ENEMY_BARS = false;

		private Camera _camera;

		private IMatchServices _matchServices;
		private IGameServices _gameServices;
		private IGameDataProvider _data;

		private readonly Dictionary<EntityRef, Transform> _anchors = new ();
		private readonly Dictionary<EntityRef, PlayerStatusBarElement> _visiblePlayers = new ();
		private readonly HashSet<EntityRef> _culledPlayers = new ();
		private readonly List<EntityRef> _entityCache = new (5);
		private readonly StyleColor _defaultShieldDmgColor = new (new Color(0.2f, 0.72f, 1f));

		// TODO: Only returned to pool when it's destroyed, and they're not culled
		private readonly Dictionary<EntityRef, HealthStatusBarElement> _healthBars = new ();

		private ObjectPool<HealthStatusBarElement> _healthBarPool;
		private ObjectPool<PlayerStatusBarElement> _playerBarPool;

		private bool _useOverheadUi;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_camera = FLGCamera.Instance.MainCamera;
			_matchServices = MainInstaller.ResolveMatchServices();
			_gameServices = MainInstaller.ResolveServices();
			_data = MainInstaller.ResolveData();

			_useOverheadUi = _data.AppDataProvider.UseOverheadUI;

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
			QuantumEvent.SubscribeManual<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.SubscribeManual<EventOnShrinkingCircleDmg>(this, OnShrinkingCircleDmg);
			QuantumEvent.SubscribeManual<EventOnCollectableBlocked>(this, OnCollectableBlocked);
			QuantumEvent.SubscribeManual<EventOnPlayerSpecialUpdated>(this, OnPlayerSpecialUpdated);
			QuantumEvent.SubscribeManual<EventOnPlayerWeaponAdded>(this, OnPlayerWeaponAdded);
			QuantumEvent.SubscribeManual<EventGameItemCollected>(this, OnCollected);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnUpdateView);
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);

			_gameServices.MessageBrokerService.UnsubscribeAll(this);
		}

		/// <summary>
		/// Forces the showing of overhead UI for local player.
		/// </summary>
		public void ForceOverheadUI()
		{
			_useOverheadUi = true;
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
				var p = dataArray[i];
				InitPlayer(f, p.Entity);
			}
		}

		private void OnPlayerSkydiveLand(EventOnPlayerSkydiveLand callback)
		{
			InitPlayer(callback.Game.Frames.Predicted, callback.Entity);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer current)
		{
			foreach (var (entity, bar) in _visiblePlayers)
			{
				var spectatingCurrentEntity = current.Entity == entity;
				if (spectatingCurrentEntity)
				{
					UpdateBarStats(QuantumRunner.Default.Game.Frames.Predicted, current.Entity, bar);	
				}
				bar.EnableStatusBars((!spectatingCurrentEntity && SHOW_ENEMY_BARS) || (spectatingCurrentEntity && _useOverheadUi));
			
			}
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

		private void InitBar(Frame f, EntityRef entity)
		{
			var bar = _playerBarPool.Get();
			_visiblePlayers.Add(entity, bar);
			UpdateBarStats(f, entity, bar);
		}

		private void UpdateBarStats(Frame f, EntityRef entity, PlayerStatusBarElement bar)
		{
			var stats = f.Get<Stats>(entity);

			var spectatingCurrentEntity = _matchServices.SpectateService.GetSpectatedEntity() == entity;

			bar.EnableStatusBars((!spectatingCurrentEntity && SHOW_ENEMY_BARS) || (spectatingCurrentEntity && _useOverheadUi));
			bar.UpdateHealth(stats.CurrentHealth, stats.CurrentHealth, stats.Values[(int) StatType.Health].StatValue.AsInt);
			bar.UpdateShield(stats.CurrentShield, stats.CurrentShield, stats.Values[(int) StatType.Shield].StatValue.AsInt);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			_culledPlayers.Remove(callback.Entity);

			if (!_visiblePlayers.Remove(callback.Entity, out var bar)) return;

			_playerBarPool.Release(bar);
		}

		private void OnPlayerLevelUp(EventOnPlayerLevelUp callback)
		{
			// TODO Can probably remove this permanently
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			if (_matchServices.IsSpectatingPlayer(callback.Entity))
			{
				bar.ShowNotification(PlayerStatusBarElement.NotificationType.LevelUp);
			}
		}

		private void OnShieldChanged(EventOnShieldChanged callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.UpdateShield(callback.PreviousShield, callback.CurrentShield, callback.CurrentShieldCapacity);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.UpdateHealth(callback.PreviousHealth, callback.CurrentHealth, callback.MaxHealth);
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
				case ConsumableType.Special:
					bar.ShowNotification(PlayerStatusBarElement.NotificationType.MaxSpecials);
					break;
				default:
					FLog.Error($"Unknown collectable: {callback.CollectableId}");
					break;
			}
		}

		private void OnPlayerSpecialUpdated(EventOnPlayerSpecialUpdated callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			if (callback.Special.IsValid && _matchServices.IsSpectatingPlayer(callback.Entity))
			{
				bar.ShowNotification(PlayerStatusBarElement.NotificationType.MiscPickup, callback.Special.SpecialId.GetLocalization());
			}
		}
		
		private void OnCollected(EventGameItemCollected ev)
		{
			if (!_matchServices.IsSpectatingPlayer(ev.PlayerEntity))
			{
				return;
			}
			if (!_visiblePlayers.TryGetValue(ev.PlayerEntity, out var bar))
			{
				return;
			}
			
			var text = $"+{ev.Amount} {ev.Collected.GetCurrencyLocalization(ev.Amount)}";
			bar.ShowNotification(PlayerStatusBarElement.NotificationType.MiscPickup, text);
		}

		private void OnPlayerWeaponAdded(EventOnPlayerWeaponAdded callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			if (_matchServices.IsSpectatingPlayer(callback.Entity))
			{
				bar.ShowNotification(PlayerStatusBarElement.NotificationType.MiscPickup, callback.Weapon.GameId.GetLocalization());
			}
		}

		private unsafe void OnPlayerAttackHit(EventOnPlayerAttackHit callback)
		{
			var f = callback.Game.Frames.Verified;
			if (callback.SpellType == Spell.KnockedOut) return;
			if (f.Has<Destructible>(callback.HitEntity) &&
				f.Unsafe.TryGetPointer<Stats>(callback.HitEntity, out var stats))

			{
				if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.HitEntity, out var view))
				{
					return;
				}

				if (!view.TryGetComponent<HealthEntityBase>(out var healthEntityBase))
				{
					return;
				}

				if (!_healthBars.TryGetValue(callback.HitEntity, out var bar))
				{
					bar = _healthBars[callback.HitEntity] = _healthBarPool.Get();
					_anchors[callback.HitEntity] = healthEntityBase.HealthBarAnchor;
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

		private void OnShrinkingCircleDmg(EventOnShrinkingCircleDmg callback)
		{
			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			if (callback.HitEntity == spectatedPlayer.Entity && _visiblePlayers.TryGetValue(callback.HitEntity, out var playerBar))
			{
				playerBar.PingDamage(callback.TotalDamage);
			}
		}
	}
}