using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Services;
using Quantum;
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
		private SpectatePlayerHealthBarObject _healthBarSpectatePlayer;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_healthBarPlayerPool = new ObjectPool<PlayerHealthBarPoolObject>(4, PlayerHealthBarInstantiator);
			_healthBarSpectatePlayer = SpectatePlayerHealthBarInstantiator();
				
			QuantumEvent.Subscribe<EventOnPlayerAttackHit>(this, OnPlayerAttackHit);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLand);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
			_services.MessageBrokerService.Subscribe<MatchEndedMessage>(OnGameplayEnded);
			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnPlayerSpectateUpdate);
			
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
			var spectateEntity = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;

			if (spectateEntity != callback.Entity)
			{
				return;
			}
			
			_healthBarSpectatePlayer.ResourceBarView.SetupView(callback.Game.Frames.Verified, callback.Entity);
			SetupHealthBar(callback.Game.Frames.Verified, callback.Entity, _healthBarSpectatePlayer);
		}

		private void OnPlayerAttackHit(EventOnPlayerAttackHit obj)
		{
			if (!_healthBarSpectatePlayer.Entity.IsValid || obj.PlayerEntity != _healthBarSpectatePlayer.Entity)
			{
				return;
			}

			var spawned = _healthBarPlayerPool.SpawnedReadOnly;

			for (var i = 0; i < spawned.Count; i++)
			{
				if (spawned[i].Entity == obj.HitEntity)
				{
					spawned[i].Despawn();
					return;
				}
			}
				
			var healthBar = _healthBarPlayerPool.Spawn();
			
			SetupHealthBar(obj.Game.Frames.Verified, obj.HitEntity, healthBar);
			healthBar.Despawn();
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			var spectateEntity = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;

			if (spectateEntity != callback.Entity)
			{
				return;
			}
			
			SetupInitialHealthBar(callback.Game.Frames.Verified, callback.Entity);
		}

		private void OnPlayerSpectateUpdate(SpectatedPlayer previousPlayer, SpectatedPlayer newPlayer)
		{
			if (!newPlayer.Entity.IsValid) return;
			
			var f = QuantumRunner.Default?.Game?.Frames.Predicted;
			var player = newPlayer.Entity.IsValid ? newPlayer : previousPlayer;

			SetupInitialHealthBar(f, player.Entity);
		}

		private async void SetupInitialHealthBar(Frame f, EntityRef playerEntity)
		{
			if (f == null || !f.TryGet<AIBlackboardComponent>(playerEntity, out var blackboard) || 
			    blackboard.GetBoolean(f, Constants.IsSkydiving) || 
			    !f.Context.GameModeConfig.SkydiveSpawn && !f.Has<AlivePlayerCharacter>(playerEntity))
			{
				_healthBarSpectatePlayer.OnDespawn();
				return;
			}

			// Sometimes there is 1-frame race condition upon reconnection/setting up the health bar, where spectated health bar
			// gets positioned incorrectly. There is most likely a better solution, but time is money, and I'm poor.
			await Task.Yield();
			
			SetupHealthBar(f, playerEntity, _healthBarSpectatePlayer);
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

			if (f.TryGet<DummyCharacter>(entity, out var dummyCharacter))
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
			_healthBarSpectatePlayer.ResourceBarView.SetupView(f, entity);
		}

		private void OnGameplayEnded(MatchEndedMessage obj)
		{
			_healthBarSpectatePlayer.OnDespawn();
			_healthBarPlayerPool.DespawnAll();
		}
		
		private SpectatePlayerHealthBarObject SpectatePlayerHealthBarInstantiator()
		{
			var instance = _healthBarSpectateRef;
		
			return new SpectatePlayerHealthBarObject
			{
				OverlayView = instance,
				HealthBar = instance.GetComponent<HealthBarView>(),
				HealthBarNameView = instance.GetComponent<HealthBarNameView>(),
				ResourceBarView = instance.GetComponent<ResourceBarView>(),
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
		
			/// <inheritdoc />
			public override void OnDespawn()
			{
				base.OnDespawn();
				ResourceBarView.OnDespawn();
			}
		}

		private class PlayerHealthBarPoolObject : IPoolEntitySpawn, IPoolEntityDespawn, IPoolEntityObject<PlayerHealthBarPoolObject>
		{
			public HealthBarNameView HealthBarNameView;
			public HealthBarShieldView HealthBarShieldView;
			public HealthBarView HealthBar;
			public OverlayWorldView OverlayView;

			private IObjectPool<PlayerHealthBarPoolObject> _pool;
			private List<Pair<Graphic, Color>> _originalGraphics = new ();

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