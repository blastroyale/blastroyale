using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This Mono Component controls the display of floating text objects using a queue or
	/// disperse movement behaviour.
	/// </summary>
	public class FloatingTextContainerView : MonoBehaviour
	{
		[Title("Colors")]
		[SerializeField] private Color _healthDamageTextColor = Color.red;
		[SerializeField] private Color _shieldDamageTextColor = new (0.6f, 0.2f, 0.9f);
		[SerializeField] private Color _ammoTextColor = Color.yellow;
		[SerializeField] private Color _neutralTextColor = Color.white;
		[SerializeField] private Color _healthGainTextColor = Color.green;
		[SerializeField] private Color _shieldGainTextColor = Color.cyan;

		[SerializeField, Required, Title("Icons")]
		private Sprite _iconArmour;

		[SerializeField, Required, Title("Refs")]
		private GameObject _infoTextRef;

		[SerializeField, Required] 
		private GameObject _statChangeTextRef;

		[SerializeField, Required, Title("Animation")]
		private MessageTypeFlotDictionary _delays;

		private IMatchServices _matchServices;
		private Coroutine _coroutine;

		private readonly Dictionary<MessageType, Queue<MessageData>> _queues = new();
		private readonly Dictionary<MessageType, Coroutine> _coroutines = new();
		private readonly Dictionary<MessageType, IObjectPool<FloatingTextPoolObject>> _pools = new();

		private void Awake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_queues.Add(MessageType.Info, new Queue<MessageData>());
			_queues.Add(MessageType.StatChange, new Queue<MessageData>());

			_pools.Add(MessageType.Info, new ObjectPool<FloatingTextPoolObject>(7, InstantiatorInfo));
			_pools.Add(MessageType.StatChange, new ObjectPool<FloatingTextPoolObject>(7, InstantiatorStatChange));

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);

			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnHealthChanged);
			QuantumEvent.Subscribe<EventOnShieldChanged>(this, OnShieldChanged);
			QuantumEvent.Subscribe<EventOnCollectableCollected>(this, OnCollectableCollected);
			QuantumEvent.Subscribe<EventOnCollectableBlocked>(this, OnCollectableBlocked);
			QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, OnPlayerEquipmentStatsChanged);
			QuantumEvent.Subscribe<EventOnPlayerDamaged>(this, OnPlayerDamaged);
		}

		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			foreach (var (_, q) in _queues)
			{
				q.Clear();
			}
		}

		private void OnCollectableBlocked(EventOnCollectableBlocked callback)
		{
			if (callback.PlayerEntity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity ||
			    !callback.Game.Frames.Verified.TryGet<Consumable>(callback.CollectableEntity, out var consumable)) return;

			var textColor = consumable.ConsumableType switch
			{
				ConsumableType.Health => _healthGainTextColor,
				ConsumableType.Ammo => _ammoTextColor,
				ConsumableType.Shield => _shieldGainTextColor,
				ConsumableType.ShieldCapacity => _shieldGainTextColor,
				_ => throw new
					     ArgumentOutOfRangeException($"Text color not defined for {consumable.ConsumableType}.")
			};

			var icon = consumable.ConsumableType is ConsumableType.Shield or ConsumableType.ShieldCapacity
				           ? _iconArmour
				           : null;
			EnqueueText(callback.PlayerEntity, "MAX", textColor, MessageType.Info, icon);
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (callback.PlayerEntity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity ||
			    callback.CollectableId != GameId.Rage) return;

			EnqueueText(callback.PlayerEntity, callback.CollectableId.GetLocalization(), _neutralTextColor, MessageType.StatChange);
		}

		private void OnShieldChanged(EventOnShieldChanged callback)
		{
			var changeShield = callback.CurrentShield - callback.PreviousShield;
			var changeCapacity = callback.CurrentShieldCapacity - callback.PreviousShieldCapacity;
			
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity || changeShield < 0)
			{
				return;
			}

			var color = changeCapacity == 0 ? _shieldGainTextColor : _neutralTextColor;
			var text = changeCapacity == 0
				           ? $"+{changeShield.ToString()}"
				           : string.Format(ScriptLocalization.AdventureMenu.Shield, changeShield.ToString());
			
			EnqueueText(callback.Entity, text, color, MessageType.Info);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			var changeValue = callback.CurrentHealth - callback.PreviousHealth;
			
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity || changeValue < 0)
			{
				return;
			}
			
			EnqueueText(callback.Entity, $"+{changeValue.ToString()}", _healthGainTextColor, MessageType.Info);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			var player = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;
			
			if (callback.TotalDamage == 0 || callback.Attacker != player && callback.Entity != player)
			{
				return;
			}

			if (callback.ShieldDamage > 0)
			{
				EnqueueText(callback.Entity, $"-{callback.ShieldDamage.ToString()}", _shieldDamageTextColor, MessageType.Info);
			}
			if (callback.HealthDamage > 0)
			{
				EnqueueText(callback.Entity, $"-{callback.HealthDamage.ToString()}", _healthDamageTextColor, MessageType.Info);
			}
		}

		private void OnPlayerEquipmentStatsChanged(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}

			var text = string.Format(ScriptLocalization.AdventureMenu.Might, callback.CurrentMight);
			
			EnqueueText(callback.Entity, text, _neutralTextColor, MessageType.StatChange);
		}

		private void EnqueueText(EntityRef playerEntity, string message, Color color, MessageType type,
		                         Sprite icon = null)
		{
			_queues[type].Enqueue(new MessageData(playerEntity, message, color, icon));

			if (!_coroutines.ContainsKey(type) && isActiveAndEnabled)
			{
				_coroutines[type] = StartCoroutine(SpawnTextCoroutine(type));
			}
		}

		private IEnumerator SpawnTextCoroutine(MessageType type)
		{
			var queue = _queues[type];
			var waitSeconds = new WaitForSeconds(_delays[type]);

			while (queue.Count > 0)
			{
				SpawnText(type, queue.Dequeue());

				yield return waitSeconds;
			}

			_coroutines.Remove(type);
		}

		private void SpawnText(MessageType type, MessageData data)
		{
			if (!TryGetSpawnPosition(data.PlayerEntity, out var position)) return;

			var textObject = _pools[type].Spawn();
			textObject.OverlayWorldView.Follow(position);
			var duration = textObject.FloatingTextView.Play(data.MessageText, data.Color, data.Icon);

			this.LateCall(duration, () => _pools[type].Despawn(textObject));
		}

		private FloatingTextPoolObject InstantiatorInfo()
		{
			return Instantiator(_infoTextRef);
		}

		private FloatingTextPoolObject InstantiatorStatChange()
		{
			return Instantiator(_statChangeTextRef);
		}

		private FloatingTextPoolObject Instantiator(GameObject refObject)
		{
			var instance = Instantiate(refObject, transform.parent, true);
			var instanceTransform = instance.transform;

			var poolObject = new FloatingTextPoolObject
			{
				FloatingTextView = instance.GetComponent<FloatingTextView>(),
				OverlayWorldView = instance.GetComponent<OverlayWorldView>()
			};

			instanceTransform.localScale = Vector3.one;
			instanceTransform.localPosition = Vector3.zero;

			instance.SetActive(false);

			return poolObject;
		}

		private bool TryGetSpawnPosition(EntityRef entity, out Vector3 position)
		{
			if (_matchServices.EntityViewUpdaterService.TryGetView(entity, out var entityView) &&
			    entityView.TryGetComponent<HealthEntityBase>(out var entityBase))
			{
				position = entityBase.HealthBarAnchor.position;
				return true;
			}

			position = default;
			return false;
		}

		private enum MessageType
		{
			StatChange,
			Info
		}

		private class FloatingTextPoolObject : IPoolEntitySpawn, IPoolEntityDespawn
		{
			public FloatingTextView FloatingTextView;
			public OverlayWorldView OverlayWorldView;

			/// <inheritdoc />
			public void OnSpawn()
			{
				FloatingTextView.gameObject.SetActive(true);
			}

			/// <inheritdoc />
			public void OnDespawn()
			{
				FloatingTextView.gameObject.SetActive(false);
			}
		}

		private struct MessageData
		{
			public EntityRef PlayerEntity;
			public string MessageText;
			public Color Color;
			public Sprite Icon;

			public MessageData(EntityRef playerEntity, string messageText, Color color, Sprite icon)
			{
				PlayerEntity = playerEntity;
				MessageText = messageText;
				Color = color;
				Icon = icon;
			}
		}

		[Serializable]
		private class MessageTypeFlotDictionary : UnitySerializedDictionary<MessageType, float>
		{
		}
	}
}