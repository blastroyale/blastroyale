using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View pools HealthBarView prefabs for given entities in our 3d world.
	/// </summary>
	public class HealthBarContainerView : MonoBehaviour
	{
		[SerializeField, Required] private OverlayWorldView _healthBarLocalPlayerRef;
		[SerializeField, Required] private OverlayWorldView _healthBarPlayerRef;
		[SerializeField, Required] private OverlayWorldView _healthBarEnemyRef;

		private IGameServices _services;
		private IObjectPool<PlayerHealthBarPoolObject> _healthBarPlayerPool;
		private LocalPlayerHealthBarPoolObject _healthBarLocalPlayer;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_healthBarPlayerPool = new ObjectPool<PlayerHealthBarPoolObject>(4, PlayerHealthBarInstantiator);
			_healthBarLocalPlayer = LocalPlayerHealthBarInstantiator();
				
			_services.MessageBrokerService.Subscribe<HealthEntityInstantiatedMessage>(OnEntityInstantiated);
			_services.MessageBrokerService.Subscribe<HealthEntityDestroyedMessage>(OnEntityDestroyed);
			_services.MessageBrokerService.Subscribe<MatchEndedMessage>(OnGameplayEnded);
			
			_healthBarLocalPlayerRef.gameObject.SetActive(false);
			_healthBarPlayerRef.gameObject.SetActive(false);
			_healthBarEnemyRef.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnEntityInstantiated(HealthEntityInstantiatedMessage message)
		{
			var frame = message.Game.Frames.Verified;
			var entity = message.Entity.EntityRef;
			var healthBar = (PlayerHealthBarPoolObject) _healthBarLocalPlayer;
			var isPlayerCharacter = frame.TryGet<PlayerCharacter>(entity, out var playerCharacter);

			if(frame.TryGet<BotCharacter>(entity, out var botCharacter))
			{
				var barCache = _healthBarPlayerPool.Spawn();
				
				barCache.HealthBarNameView.NameText.text = Extensions.GetBotName(botCharacter.BotNameIndex.ToString());
				
				healthBar = barCache;
			}
			else if(message.Game.PlayerIsLocal(playerCharacter.Player))
			{
				var playerData = frame.GetPlayerData(playerCharacter.Player);
				
				_healthBarLocalPlayer.HealthBarNameView.NameText.text = playerData.PlayerName;
				
				_healthBarLocalPlayer.HealthBarTextView.SetupView(entity, frame.Get<Stats>(entity).CurrentHealth);
				_healthBarLocalPlayer.ReloadBarView.SetupView(frame, entity);
			}
			else
			{
				var barCache = _healthBarPlayerPool.Spawn();
				var playerName = isPlayerCharacter ? frame.GetPlayerData(playerCharacter.Player).PlayerName : "";
				
				barCache.HealthBarNameView.NameText.text = playerName;
				healthBar = barCache;
			}

			healthBar.HealthBarInterimArmourView.SetupView(entity, frame.Get<Stats>(entity).CurrentInterimArmour);
			
			SetupHealthBar(frame, message.Entity, healthBar);
		}

		private void OnEntityDestroyed(HealthEntityDestroyedMessage message)
		{
			_healthBarPlayerPool.Despawn(true, x => x.HealthBar.Entity == message.Entity.EntityRef);
		}

		private void OnGameplayEnded(MatchEndedMessage obj)
		{
			_healthBarPlayerPool.DespawnAll();
			_healthBarLocalPlayer.OnDespawn();
		}
		
		private void SetupHealthBar(Frame f, EntityView entityView, HealthBarPoolObject healthBar)
		{
			var anchor = entityView.GetComponent<HealthEntityBase>().HealthBarAnchor;
			var stats = f.Get<Stats>(entityView.EntityRef);
			var maxHealth = stats.Values[(int) StatType.Health].StatValue.AsInt;
			
			healthBar.HealthBar.SetupView(entityView.EntityRef, stats.CurrentHealth, maxHealth);
			healthBar.OverlayView.Follow(anchor);
		}
		
		private LocalPlayerHealthBarPoolObject LocalPlayerHealthBarInstantiator()
		{
			var instance = _healthBarLocalPlayerRef;
		
			return new LocalPlayerHealthBarPoolObject
			{
				OverlayView = instance,
				HealthBar = instance.GetComponent<HealthBarView>(),
				HealthBarNameView = instance.GetComponent<HealthBarNameView>(),
				HealthBarTextView = instance.GetComponent<HealthBarTextView>(),
				ReloadBarView = instance.GetComponent<ReloadBarView>(),
				HealthBarInterimArmourView = instance.GetComponent<HealthBarInterimArmourView>()
			};
		}
		
		private PlayerHealthBarPoolObject PlayerHealthBarInstantiator()
		{
			var instance = HealthBarPoolObjectInstantiator(_healthBarPlayerRef);
		
			return new PlayerHealthBarPoolObject
			{
				OverlayView = instance,
				HealthBar = instance.GetComponent<HealthBarView>(),
				HealthBarNameView = instance.GetComponent<HealthBarNameView>(),
				HealthBarInterimArmourView = instance.GetComponent<HealthBarInterimArmourView>()
			};
		}

		private OverlayWorldView HealthBarPoolObjectInstantiator(OverlayWorldView healthBarRef)
		{
			var instance = Instantiate(healthBarRef, transform, true);
			var instanceTransform = instance.transform;
		
			instance.gameObject.SetActive(false);

			instanceTransform.localPosition = Vector3.zero;
			instanceTransform.localScale = Vector3.one;

			return instance;
		}
		
		private class LocalPlayerHealthBarPoolObject : PlayerHealthBarPoolObject
		{
			public ReloadBarView ReloadBarView;
			public HealthBarTextView HealthBarTextView;
		
			/// <inheritdoc />
			public override void OnDespawn()
			{
				base.OnDespawn();
				ReloadBarView.OnDespawn();
				HealthBarTextView.OnDespawn();
			}
		}

		private class PlayerHealthBarPoolObject : HealthBarPoolObject
		{
			public HealthBarNameView HealthBarNameView;
			public HealthBarInterimArmourView HealthBarInterimArmourView;
		
			/// <inheritdoc />
			public override void OnDespawn()
			{
				base.OnDespawn();
				HealthBarInterimArmourView.OnDespawn();
			}
		}

		private class HealthBarPoolObject : IPoolEntityDespawn
		{
			public HealthBarView HealthBar;
			public OverlayWorldView OverlayView;
		 	
			/// <inheritdoc />
			public virtual void OnDespawn()
			{
				HealthBar.gameObject.SetActive(false);
				HealthBar.OnDespawn();
				OverlayView.OnDespawn();
			}
		}
	}
}