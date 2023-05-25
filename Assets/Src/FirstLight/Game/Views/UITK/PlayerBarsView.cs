using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles displaying the player bars on the screen.
	/// </summary>
	public class PlayerBarsView : UIView
	{
		private Camera _camera;

		private IGameServices _gameServices;
		private IMatchServices _matchServices;

		private readonly Dictionary<EntityRef, Transform> _anchors = new();
		private readonly Dictionary<EntityRef, PlayerBarElement> _visiblePlayers = new();
		private readonly HashSet<EntityRef> _culledPlayers = new();
		private readonly List<EntityRef> _entityCache = new(5);

		private ObjectPool<PlayerBarElement> _barPool;


		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_camera = Camera.main;
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			element.Clear();

			_barPool = new ObjectPool<PlayerBarElement>(
				() =>
				{
					var pbe = new PlayerBarElement();
					pbe.SetDisplay(false);
					Element.Add(pbe);
					return pbe;
				},
				pbe => pbe.SetDisplay(true),
				pbe => pbe.SetDisplay(false),
				pbe => pbe.RemoveFromHierarchy(),
				true, 3);
		}

		public override void SubscribeToEvents()
		{
			_gameServices.TickService.SubscribeOnLateUpdate(OnUpdate);
			QuantumEvent.SubscribeManual<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLand);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnHealthChanged>(this, OnHealthChanged);
			QuantumEvent.SubscribeManual<EventOnShieldChanged>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerLevelUp>(this, OnPlayerLevelUp);
			QuantumEvent.SubscribeManual<EventOnPlayerMagazineChanged>(this, OnPlayerMagazineChanged);
		}

		public override void UnsubscribeFromEvents()
		{
			_gameServices.TickService.UnsubscribeOnLateUpdate(OnUpdate);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnUpdate(float _)
		{
			var f = QuantumRunner.Default.Game.Frames.Predicted;

			// Cull players that are currently not culled but should be
			_entityCache.Clear();
			foreach (var (player, value) in _visiblePlayers)
			{
				if (!f.IsCulled(player)) continue;

				_culledPlayers.Add(player);
				_barPool.Release(value);

				_entityCache.Add(player);
			}

			foreach (var entity in _entityCache)
			{
				_visiblePlayers.Remove(entity);
			}

			// Un-Cull players that are currently culled but they shouldn't be
			_entityCache.Clear();
			foreach (var player in _culledPlayers)
			{
				if (f.IsCulled(player)) continue;

				var bar = _barPool.Get();
				_visiblePlayers.Add(player, _barPool.Get());
				SetupBar(f, player, bar);
				_entityCache.Add(player);
			}

			foreach (var entity in _entityCache)
			{
				_culledPlayers.Remove(entity);
			}

			// Update all position of UI elements of all the un-culled players.
			foreach (var (entity, bar) in _visiblePlayers)
			{
				var anchor = _anchors[entity];
				var screenPoint = _camera.WorldToScreenPoint(anchor.position);
				screenPoint.y = _camera.pixelHeight - screenPoint.y;

				var panelPos = RuntimePanelUtils.ScreenToPanel(Element.panel, screenPoint);
				bar.transform.position = panelPos;
			}
		}

		private void OnPlayerSkydiveLand(EventOnPlayerSkydiveLand callback)
		{
			// TODO: Temp for testing
			// if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity != callback.Entity) return;

			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var view)) return;

			var f = callback.Game.Frames.Verified;

			_anchors.Add(callback.Entity, view.GetComponent<HealthEntityBase>().HealthBarAnchor);

			FLog.Info("PACO", $"SkydiveLandCulled: {f.IsCulled(callback.Entity)}");
			
			if (f.IsCulled(callback.Entity))
			{
				_culledPlayers.Add(callback.Entity);
				return;
			}

			var bar = _barPool.Get();
			_visiblePlayers.Add(callback.Entity, bar);
			SetupBar(callback.Game.Frames.Verified, callback.Entity, bar);
		}

		private void OnCulledChanged(EntityRef entity, bool culled)
		{
			FLog.Info("PACO", $"Culled changed: {entity}-{culled}");
		}

		private void SetupBar(Frame f, EntityRef entity, PlayerBarElement bar)
		{
			var pc = f.Get<PlayerCharacter>(entity);
			var stats = f.Get<Stats>(entity);

			var playerName = f.TryGet<BotCharacter>(entity, out var botCharacter)
				? Extensions.GetBotName(botCharacter.BotNameIndex, entity)
				: f.GetPlayerData(pc.Player).PlayerName;

			bar.SetName(playerName);
			bar.SetLevel(pc.GetEnergyLevel(f));
			bar.SetHealth(stats.CurrentHealth, stats.CurrentHealth,
				stats.Values[(int) StatType.Health].StatValue.AsInt);
			bar.SetShield(stats.CurrentShield, stats.Values[(int) StatType.Shield].StatValue.AsInt);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			_culledPlayers.Remove(callback.Entity);

			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			_visiblePlayers.Remove(callback.Entity);
			_barPool.Release(bar);
		}

		private void OnPlayerLevelUp(EventOnPlayerLevelUp callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			bar.SetLevel(callback.CurrentLevel);
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

		private void OnPlayerMagazineChanged(EventOnPlayerMagazineChanged callback)
		{
			if (!_visiblePlayers.TryGetValue(callback.Entity, out var bar)) return;

			// FLog.Info("PACO", $"MagChanged {callback.ShotCount}, {callback.MagSize}");

			bar.SetMagazine(callback.ShotCount, callback.MagSize);
		}
	}
}