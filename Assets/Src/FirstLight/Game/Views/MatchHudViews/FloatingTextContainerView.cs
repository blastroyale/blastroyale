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
		[SerializeField, Title("Colors")] private Color _hitTextColor = Color.red;
		[SerializeField] private Color _healTextColor = Color.green;
		[SerializeField] private Color _neutralTextColor = Color.white;
		[SerializeField] private Color _armourGainTextColor = Color.cyan;

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
			QuantumEvent.Subscribe<EventOnPlayerStatsChanged>(this, OnPlayerStatsChanged);
			QuantumEvent.Subscribe<EventOnPlayerDamaged>(this, OnPlayerDamaged);
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
			if (callback.PlayerEntity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			if (callback.Game.Frames.Verified.TryGet<Consumable>(callback.CollectableEntity, out var consumable))
			{
				var textColor = consumable.ConsumableType switch
				{
					ConsumableType.Health => _healTextColor,
					ConsumableType.Ammo => _neutralTextColor,
					ConsumableType.Shield => _armourGainTextColor,
					ConsumableType.ShieldCapacity => _armourGainTextColor,
					_ => throw new
						     ArgumentOutOfRangeException($"Text color not defined for {consumable.ConsumableType}.")
				};

				EnqueueText(callback.PlayerEntity, "MAX", textColor, MessageType.Info,
				            consumable.ConsumableType is ConsumableType.Shield or ConsumableType.ShieldCapacity
					            ? _iconArmour
					            : null);
			}
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (callback.PlayerEntity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;
			if (callback.CollectableId != GameId.Rage && !callback.CollectableId.IsInGroup(GameIdGroup.Weapon)) return;

			EnqueueText(callback.PlayerEntity, callback.CollectableId.GetTranslation(), _neutralTextColor, MessageType.StatChange);
		}

		private void OnShieldChanged(EventOnShieldChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}

			var changeValue = callback.CurrentShield - callback.PreviousShield;
			var color = changeValue < 0 ? _hitTextColor : _armourGainTextColor;
			
			EnqueueText(callback.Entity, changeValue.ToString(), color, MessageType.Info);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}

			var changeValue = callback.CurrentHealth - callback.PreviousHealth;
			var color = changeValue < 0 ? _hitTextColor : _healTextColor;
			
			EnqueueText(callback.Entity, changeValue.ToString(), color, MessageType.Info);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			if (callback.Attacker != _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}
			
			EnqueueValue(callback.Entity, "", (int) -callback.TotalDamage, MessageType.Info);
		}

		private void OnPlayerStatsChanged(EventOnPlayerStatsChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			// Don't show stat changes in Deathmatch game mode
			if (callback.Game.Frames.Verified.Context.MapConfig.GameMode == GameMode.Deathmatch) return;

			for (int i = 0; i < Constants.TOTAL_STATS; i++)
			{
				var difference = callback.CurrentStats.Values[i].BaseValue.AsFloat -
				                 callback.PreviousStats.Values[i].BaseValue.AsFloat;
				var statName = callback.CurrentStats.Values[i].Type.GetTranslation();
				
				EnqueueValue(callback.Entity, statName, Mathf.RoundToInt(difference), MessageType.StatChange);
			}
		}

		private void EnqueueValue(EntityRef playerEntity, string valueName, int value, MessageType type)
		{
			if (value == 0) return;

			var valueSign = value > 0 ? "+" : "";
			var messageColor = value > 0 ? _neutralTextColor : _hitTextColor;

			EnqueueText(playerEntity, $"{valueName} {valueSign}{value.ToString()}", messageColor, type);
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
			var delay = _delays[type];

			while (queue.Count > 0)
			{
				SpawnText(type, queue.Dequeue());

				yield return new WaitForSeconds(delay);
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

		private struct FloatingTextPoolObject : IPoolEntitySpawn, IPoolEntityDespawn
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