using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Services;
using Quantum;
using Quantum.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This View pools HealthBarView prefabs for given entities in our 3d world.
	/// </summary>
	public class HealthBarContainerView : MonoBehaviour
	{
		[SerializeField, Required] private OverlayWorldView _healthBarSpectateRef;
		[SerializeField, Required] private OverlayWorldView _healthBarRef;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private IObjectPool<PlayerHealthBarPoolObject> _healthBarPlayerPool;
		private Dictionary<EntityRef, SpectatePlayerHealthBarObject> _friendlyHealthBars;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_healthBarPlayerPool = new ObjectPool<PlayerHealthBarPoolObject>(4, PlayerHealthBarInstantiator);
			_friendlyHealthBars = new Dictionary<EntityRef, SpectatePlayerHealthBarObject>();

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnPlayerSpectateUpdate);
			_services.MessageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnded);
			QuantumEvent.Subscribe<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLand);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);

			_healthBarSpectateRef.gameObject.SetActive(false);
			_healthBarRef.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnPlayerSpectateUpdate);
		}

		private void OnPlayerSkydiveLand(EventOnPlayerSkydiveLand callback)
		{
			TryShowHealthBar(callback.Game.Frames.Verified, callback.Entity);
		}

		private void OnPlayerAttackHit(EventOnPlayerAttackHit e)
		{
			if (!_friendlyHealthBars.ContainsKey(e.PlayerEntity))
			{
				return;
			}

			var spawned = _healthBarPlayerPool.SpawnedReadOnly;

			for (var i = 0; i < spawned.Count; i++)
			{
				if (spawned[i].Entity == e.HitEntity)
				{
					spawned[i].Despawn();
					return;
				}
			}

			var healthBar = _healthBarPlayerPool.Spawn();

			SetupHealthBar(e.Game.Frames.Verified, e.HitEntity, healthBar);
			healthBar.Despawn();
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (!callback.Game.Frames.Verified.Context.GameModeConfig.SkydiveSpawn)
			{
				TryShowHealthBar(callback.Game.Frames.Verified, callback.Entity);
			}
		}

		private void TryShowHealthBar(Frame f, EntityRef entity)
		{
			if (ShouldShowHealthbar(f, entity) && !_friendlyHealthBars.ContainsKey(entity))
			{
				FLog.Info("PACO", "ShowingHealthbar!!!!");

				var healthBar = FriendlyPlayerHealthBarInstantiator();
				_friendlyHealthBars.Add(entity, healthBar);
				SetupFriendlyHealthBar(f, entity, healthBar);
			}
		}

		private bool ShouldShowHealthbar(Frame f, EntityRef entity)
		{
			var spectatePlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			return
				f.Has<AlivePlayerCharacter>(entity) &&
				f.TryGet<PlayerCharacter>(entity, out var pc) && !pc.IsSkydiving(f, entity) &&
				f.TryGet<Targetable>(entity, out var t) && t.Team == spectatePlayer.Team;
		}

		private void OnPlayerSpectateUpdate(SpectatedPlayer previousPlayer, SpectatedPlayer newPlayer)
		{
			foreach (var (_, hb) in _friendlyHealthBars)
			{
				hb.OnDespawn();
			}

			_friendlyHealthBars.Clear();

			if (newPlayer.Entity.IsValid)
			{
				var f = QuantumRunner.Default.Game.Frames.Verified;

				foreach (var (entity, t) in f.GetComponentIterator<Targetable>())
				{
					TryShowHealthBar(f, entity);
				}
			}
		}

		private async void SetupFriendlyHealthBar(Frame f, EntityRef playerEntity,
												  SpectatePlayerHealthBarObject healthBar)
		{
			FLog.Info("PACO", "SettingUpFriendlyHealthbar 1");
			if (!f.TryGet<PlayerCharacter>(playerEntity, out var playerCharacter))
			{
				healthBar.OnDespawn();
				return;
			}

			// Sometimes there is 1-frame race condition upon reconnection/setting up the health bar, where spectated health bar
			// gets positioned incorrectly. There is most likely a better solution, but time is money, and I'm poor.
			await Task.Yield();

			FLog.Info("PACO", "SettingUpFriendlyHealthbar 2");
			healthBar.ResourceBarView.SetupView(f, playerCharacter, playerEntity);
			healthBar.ReloadBarView.SetupView(f, playerCharacter, playerEntity);
			SetupHealthBar(f, playerEntity, healthBar);
		}

		private void SetupHealthBar(Frame f, EntityRef entity, PlayerHealthBarPoolObject healthBar)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(entity, out var entityView) ||
				!f.TryGet<Stats>(entity, out var stats))
			{
				healthBar.OnDespawn();
				return;
			}

			var anchor = entityView.GetComponent<HealthEntityBase>().HealthBarAnchor;
			var maxHealth = stats.Values[(int) StatType.Health].StatValue.AsInt;

			if (f.Has<Destructible>(entity))
			{
				healthBar.HealthBarNameView.NameText.text = "";
			}
			if (f.Has<DummyCharacter>(entity))
			{
				healthBar.HealthBarNameView.NameText.text = "Dummy " + entity.Index;

			}
			else if (f.TryGet<PlayerCharacter>(entity, out var playerCharacter))
			{
				var playerName = f.TryGet<BotCharacter>(entity, out var botCharacter)
					? Extensions.GetBotName(botCharacter.BotNameIndex.ToString())
					: f.GetPlayerData(playerCharacter.Player).PlayerName;

				healthBar.HealthBarNameView.NameText.text = playerName;
			}

			healthBar.OverlayView.gameObject.SetActive(true);
			healthBar.HealthBar.SetupView(entity, stats.CurrentHealth, maxHealth);
			healthBar.HealthBarShieldView.SetupView(entity, stats.CurrentShield);
			healthBar.OverlayView.Follow(anchor);
		}

		private void OnMatchEnded(MatchEndedMessage msg)
		{
			foreach (var (_, hb) in _friendlyHealthBars)
			{
				hb.OnDespawn();
			}

			_healthBarPlayerPool.DespawnAll();
		}

		private SpectatePlayerHealthBarObject FriendlyPlayerHealthBarInstantiator()
		{
			var instance = Instantiate(_healthBarSpectateRef, transform, true);
			;

			return new SpectatePlayerHealthBarObject
			{
				OverlayView = instance,
				HealthBar = instance.GetComponent<HealthBarView>(),
				HealthBarNameView = instance.GetComponent<HealthBarNameView>(),
				ResourceBarView = instance.GetComponent<ResourceBarView>(),
				ReloadBarView = instance.GetComponent<ReloadBarView>(),
				HealthBarShieldView = instance.GetComponent<HealthBarShieldView>()
			};
		}

		private PlayerHealthBarPoolObject PlayerHealthBarInstantiator()
		{
			var instance = Instantiate(_healthBarRef, transform, true);
			var instanceTransform = instance.transform;

			instance.gameObject.SetActive(false);

			instanceTransform.localPosition = Vector3.zero;
			instanceTransform.localScale = Vector3.one;

			return new PlayerHealthBarPoolObject
			{
				OverlayView = instance,
				HealthBar = instance.GetComponent<HealthBarView>(),
				HealthBarNameView = instance.GetComponent<HealthBarNameView>(),
				HealthBarShieldView = instance.GetComponent<HealthBarShieldView>()
			};
		}

		private class SpectatePlayerHealthBarObject : PlayerHealthBarPoolObject
		{
			public ResourceBarView ResourceBarView;
			public ReloadBarView ReloadBarView;

			/// <inheritdoc />
			public override void OnDespawn()
			{
				base.OnDespawn();
				ResourceBarView.OnDespawn();
				ReloadBarView.OnDespawn();
			}
		}

		private class PlayerHealthBarPoolObject : IPoolEntitySpawn, IPoolEntityDespawn,
												  IPoolEntityObject<PlayerHealthBarPoolObject>
		{
			public HealthBarNameView HealthBarNameView;
			public HealthBarShieldView HealthBarShieldView;
			public HealthBarView HealthBar;
			public OverlayWorldView OverlayView;

			private IObjectPool<PlayerHealthBarPoolObject> _pool;
			private List<Pair<Graphic, Color>> _originalGraphics = new();

			/// <summary>
			/// The current reference entity
			/// </summary>
			public EntityRef Entity => HealthBar.Entity;

			public void OnSpawn()
			{
				foreach (var pair in _originalGraphics)
				{
					pair.Key.color = pair.Value;
				}

				_originalGraphics.Clear();
			}

			/// <inheritdoc />
			public virtual void OnDespawn()
			{
				HealthBar.gameObject.SetActive(false);
				HealthBar.OnDespawn();
				OverlayView.OnDespawn();
				HealthBarShieldView.OnDespawn();
			}

			/// <inheritdoc />
			public void Init(IObjectPool<PlayerHealthBarPoolObject> pool)
			{
				_pool = pool;
			}

			/// <inheritdoc />
			public bool Despawn()
			{
				var graphics = OverlayView.Graphics;
				var isEmpty = _originalGraphics.Count == 0;

				foreach (var pair in _originalGraphics)
				{
					pair.Key.color = pair.Value;
				}

				for (var i = 0; i < graphics.Count; i++)
				{
					if (isEmpty)
					{
						_originalGraphics.Add(new Pair<Graphic, Color>(graphics[i], graphics[i].color));
					}

					graphics[i].DOKill();
					var tween = graphics[i].DOFade(0, GameConstants.Visuals.GAMEPLAY_POST_ATTACK_HIDE_DURATION)
						.SetEase(Ease.InCubic);

					if (i == 0)
					{
						tween.OnComplete(() => _pool?.Despawn(this));
					}
				}

				return true;
			}
		}
	}
}