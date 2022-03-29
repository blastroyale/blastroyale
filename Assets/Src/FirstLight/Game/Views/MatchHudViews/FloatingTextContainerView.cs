using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This Mono Component controls the display of floating text objects using a queue or
	/// disperse movement behaviour.
	/// </summary>
	public class FloatingTextContainerView : MonoBehaviour
	{
		[SerializeField] private float _queueWaitTime = 0.5f;
		[SerializeField] private Color _hitTextColor = Color.red;
		[SerializeField] private Color _healTextColor = Color.green;
		[SerializeField] private Color _neutralTextColor = Color.white;
		[SerializeField] private Color _armourLossTextColor = Color.white;
		[SerializeField] private Color _armourGainTextColor = Color.cyan;
		[SerializeField] private AnimationClip _floatingTextAnimation;
		[SerializeField] private AnimationClip _floatingArmourAndTextAnimation;
		[SerializeField] private AnimationClip _floatingFastTextAnimation;
		[SerializeField] private AnimationClip _floatingFastArmourAndTextAnimation;
		[SerializeField] private GameObject _floatingTextRef;
		[SerializeField] private GameObject _floatingArmourAndTextRef;
		
		private IGameServices _services;
		private IObjectPool<FloatingTextPoolObject> _pool;
		private IObjectPool<FloatingTextPoolObject> _poolArmour;
		private Coroutine _coroutine;
		
		private readonly IDictionary<EntityRef, Queue<MessageData>> _queue = new Dictionary<EntityRef, Queue<MessageData>>(7);
		
		private void Awake()
		{
			_floatingTextRef.gameObject.SetActive(false);
			_floatingArmourAndTextRef.gameObject.SetActive(false);
			
			_services = MainInstaller.Resolve<IGameServices>();
			_pool = new ObjectPool<FloatingTextPoolObject>(7, InstantiatorNormal);
			_poolArmour = new ObjectPool<FloatingTextPoolObject>(7, InstantiatorArmour);
			
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
			QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
			QuantumEvent.Subscribe<EventOnHealthChanged>(this, OnHealthUpdate);
			QuantumEvent.Subscribe<EventOnLocalCollectableCollected>(this, OnLocalCollectableCollected);
			QuantumEvent.Subscribe<EventOnInterimArmourChanged>(this, OnInterimArmourUpdate);
		}

		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_queue.Remove(callback.Entity);
		}
		
		private void OnEventOnPlayerLeft(EventOnPlayerLeft callback)
		{
			_queue.Remove(callback.Entity);
		}
		
		private void OnLocalCollectableCollected(EventOnLocalCollectableCollected callback)
		{
			if (!_services.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView) || 
			    !entityView.TryGetComponent<HealthEntityBase>(out var entityBase))
			{
				Debug.LogWarning($"The entity {callback.PlayerEntity} is not ready for a losing floating text yet");
				return;
			}
			
			EnqueueText(_pool, entityBase, callback.CollectableId.GetTranslation(), _neutralTextColor, _floatingTextAnimation);
		}

		private void OnInterimArmourUpdate(EventOnInterimArmourChanged callback)
		{
			if (callback.PreviousInterimArmour == callback.CurrentInterimArmour)
			{
				return;
			}

			OnValueUpdated(callback.Game, callback.Entity, callback.Attacker, callback.PreviousInterimArmour,
			               callback.CurrentInterimArmour, _poolArmour, _armourLossTextColor, _armourGainTextColor,
			               _floatingFastArmourAndTextAnimation, _floatingArmourAndTextAnimation);
		}
		
		private void OnHealthUpdate(EventOnHealthChanged callback)
		{
			if (callback.PreviousHealth == callback.CurrentHealth || callback.PreviousHealth == 0)
			{
				return;
			}

			OnValueUpdated(callback.Game, callback.Entity, callback.Attacker, callback.PreviousHealth,
			               callback.CurrentHealth, _pool, _hitTextColor, _healTextColor,
			               _floatingFastTextAnimation, _floatingTextAnimation);
		}

		private void OnValueUpdated(QuantumGame game, EntityRef victim, EntityRef attacker, int previousValue, int currentValue,
		                            IObjectPool<FloatingTextPoolObject> pool, Color loseColor, Color gainColor,
		                            AnimationClip loseAnimation, AnimationClip gainAnimation)
		{
			var frame = game.Frames.Verified;

			// Checks if the health affected is somehow the local player
			if ((!frame.TryGet<PlayerCharacter>(attacker, out var attackerPlayer) || !game.PlayerIsLocal(attackerPlayer.Player)) &&
				(!frame.TryGet<PlayerCharacter>(victim, out var victimPlayer) || !game.PlayerIsLocal(victimPlayer.Player)))
			{
				return;
			}

			if (!_services.EntityViewUpdaterService.TryGetView(victim, out var entityView) || 
			    !entityView.TryGetComponent<HealthEntityBase>(out var entityBase))
			{
				Debug.LogWarning($"The entity {victim} is not ready for a losing floating text yet");
				return;
			}

			if (previousValue > currentValue)
			{
				SpawnText(pool,(previousValue - currentValue).ToString(), loseColor, 
				          entityBase.HealthBarAnchor.position, 1.5f);
			}
			else
			{
				SpawnText(pool,(currentValue - previousValue).ToString(), gainColor, 
				          entityBase.HealthBarAnchor.position, 1.5f);
			}
		}

		private void EnqueueText(IObjectPool<FloatingTextPoolObject> pool, HealthEntityBase entityBase, string message, Color color, AnimationClip flyingAnimation)
		{
			var messageData = new MessageData
			{
				Anchor = entityBase.HealthBarAnchor,
				MessageText = message, 
				Color = color,
				Pool = pool
			};
			
			if (!_queue.TryGetValue(entityBase.EntityView.EntityRef, out var queue))
			{
				queue = new Queue<MessageData>();
				
				_queue.Add(entityBase.EntityView.EntityRef, queue);
			}
			
			queue.Enqueue(messageData);
			
			if (_coroutine == null)
			{
				_coroutine = StartCoroutine(SpawnTextCoroutine(queue));
			}
		}
		
		private IEnumerator SpawnTextCoroutine(Queue<MessageData> queue)
		{
			while (queue.Count > 0)
			{
				var messageData = queue.Peek();
				
				SpawnText(messageData.Pool, messageData.MessageText, messageData.Color, 
				          messageData.Anchor.position, 1.5f);

				yield return new WaitForSeconds(_queueWaitTime);

				queue.Dequeue();
			}

			_coroutine = null;
		}
		
		private void SpawnText(IObjectPool<FloatingTextPoolObject> pool, string text, Color color, Vector3 position, float duration)
		{
			var closurePool = pool;
			var floatingTextPoolObject = pool.Spawn();

			floatingTextPoolObject.OverlayWorldView.Follow(position);
			floatingTextPoolObject.FloatingTextView.Play(text, color, duration);
			
			this.LateCall(duration, () => closurePool.Despawn(floatingTextPoolObject));
		}
		
		private FloatingTextPoolObject InstantiatorNormal()
		{
			return Instantiator(_floatingTextRef);
		}
		
		private FloatingTextPoolObject InstantiatorArmour()
		{
			return Instantiator(_floatingArmourAndTextRef);
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
			public Transform Anchor; 
			public string MessageText;
			public Color Color;
			public IObjectPool<FloatingTextPoolObject> Pool;
		}
	}
}