using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// The Tick service provides updatable calls to other objects. It process the OnUpdate, OnLateUpdate & OnFixedUpdate calls.
	/// It allows pure C# objects to have updatable calls and also removes the overhead of using the MonoBehaviour update methods
	/// over multiple GameObjects.
	/// The Tick Service creates a game object in the scene that will be the true container of all update calls to be executed
	/// through this service. It also keeps the update data on scene load/unload.
	/// Call <see cref="Dispose"/> to clear the Tick Service correctly.
	/// </summary>
	public interface ITickService
	{
		/// <summary>
		/// Subscribes the <paramref name="action"/> to the frame based update with a <paramref name="deltaTime"/> buffer
		/// between each call, with the option to use <paramref name="realTime"/> or game time (Game time can be manipulated to
		/// run faster or slower).
		/// It has the option to set <paramref name="timeOverflowToNextTick"/> in order to invoke each <paramref name="action"/> close to
		/// the <paramref name="deltaTime"/> defined. This is because the updates don't run always in the same exact delta time
		/// and if the last frame took longer to be processed it will be taken into consideration.
		/// </summary>
		void SubscribeOnUpdate(Action<float> action, float deltaTime = 0f, bool timeOverflowToNextTick = false, bool realTime = false);
		
		/// <summary>
		/// Subscribes the <paramref name="action"/> to the frame based after the main update with a <paramref name="deltaTime"/> buffer
		/// between each call, with the option to use <paramref name="realTime"/> or game time (Game time can be manipulated to
		/// run faster or slower).
		/// It has the option to set <paramref name="timeOverflowToNextTick"/> in order to invoke each <paramref name="action"/> close to
		/// the <paramref name="deltaTime"/> defined. This is because the updates don't run always in the same exact delta time
		/// and if the last frame took longer to be processed it will be taken into consideration.
		/// </summary>
		void SubscribeOnLateUpdate(Action<float> action, float deltaTime = 0f, bool timeOverflowToNextTick = false, bool realTime = false);
		
		/// <summary>
		/// Subscribes the <paramref name="action"/> to the fixed update
		/// </summary>
		void SubscribeOnFixedUpdate(Action<float> action);
		
		/// <summary>
		/// Unsubscribes the <paramref name="action"/> from any of the update
		/// </summary>
		void Unsubscribe(Action<float> action);
		
		/// <summary>
		/// Unsubscribes the <paramref name="action"/> from on update
		/// </summary>
		void UnsubscribeOnUpdate(Action<float> action);
		
		/// <summary>
		/// Unsubscribes the <paramref name="action"/> from on fixed update call
		/// </summary>
		void UnsubscribeOnFixedUpdate(Action<float> action);
		
		/// <summary>
		/// Unsubscribes the <paramref name="action"/> from on late update call
		/// </summary>
		void UnsubscribeOnLateUpdate(Action<float> action);
		
		/// <summary>
		/// Unsubscribes from all on updates
		/// </summary>
		void UnsubscribeAllOnUpdate();
		
		/// <summary>
		/// Unsubscribes from all on updates from the given <paramref name="subscriber"/>
		/// </summary>
		void UnsubscribeAllOnUpdate(object subscriber);
		
		/// <summary>
		/// Unsubscribes from all on fixed updates
		/// </summary>
		void UnsubscribeAllOnFixedUpdate();
		
		/// <summary>
		/// Unsubscribes from all on fixed updates from the given <paramref name="subscriber"/>
		/// </summary>
		void UnsubscribeAllOnFixedUpdate(object subscriber);
		
		/// <summary>
		/// Unsubscribes from all on fixed updates
		/// </summary>
		void UnsubscribeAllOnLateUpdate();
		
		/// <summary>
		/// Unsubscribes from all on fixed updates from the given <paramref name="subscriber"/>
		/// </summary>
		void UnsubscribeAllOnLateUpdate(object subscriber);
		
		/// <summary>
		/// Unsubscribes from all updates
		/// </summary>
		void UnsubscribeAll();
		
		/// <summary>
		/// Unsubscribes from all updates from the given <paramref name="subscriber"/>
		/// </summary>
		void UnsubscribeAll(object subscriber);
	}

	/// <inheritdoc cref="ITickService"/>
	public class TickService : ITickService, IDisposable
	{
		private readonly TickServiceMonoBehaviour _tickObject;

		private readonly List<TickData> _onUpdateList = new List<TickData>();
		private readonly List<TickData> _onFixedUpdateList = new List<TickData>();
		private readonly List<TickData> _onLateUpdateList = new List<TickData>();

		private int _tickDataIdRef;
		
		public TickService()
		{
			if (_tickObject != null)
			{
				throw new InvalidOperationException("The tick service is being initialized for the second time and that is not valid");
			}

			var gameObject = new GameObject(typeof(TickServiceMonoBehaviour).Name);
			
			Object.DontDestroyOnLoad(gameObject);

			_tickObject = gameObject.AddComponent<TickServiceMonoBehaviour>();
			_tickObject.OnUpdate = OnUpdate;
			_tickObject.OnFixedUpdate = OnFixedUpdate;
			_tickObject.OnLateUpdate = OnLateUpdate;
		}

		/// <summary>
		/// Cleans the tick service and deletes the tick game object that contains all the update calls in the game.
		/// This will also stop all currently running updates.
		/// </summary>
		public void Dispose()
		{
			Object.Destroy(_tickObject.gameObject);

			_onUpdateList.Clear();
			_onFixedUpdateList.Clear();
			_onLateUpdateList.Clear();
		}

		/// <inheritdoc />
		public void SubscribeOnUpdate(Action<float> action, float deltaTime = 0f, bool timeOverflowToNextTick = false, bool realTime = false)
		{
			_onUpdateList.Add(new TickData
			{
				Id = ++_tickDataIdRef,
				Action = action,
				DeltaTime = deltaTime,
				TimeOverflowToNextTick = timeOverflowToNextTick,
				RealTime = realTime,
				LastTickTime = realTime ? Time.realtimeSinceStartup : Time.time,
				Subscriber = action.Target
			});
		}

		/// <inheritdoc />
		public void SubscribeOnLateUpdate(Action<float> action, float deltaTime = 0f, bool timeOverflowToNextTick = false, bool realTime = false)
		{
			_onLateUpdateList.Add(new TickData
			{
				Id = ++_tickDataIdRef,
				Action = action,
				DeltaTime = deltaTime,
				TimeOverflowToNextTick = timeOverflowToNextTick,
				RealTime = realTime,
				LastTickTime = realTime ? Time.realtimeSinceStartup : Time.time,
				Subscriber = action.Target
			});
		}

		/// <inheritdoc />
		public void SubscribeOnFixedUpdate(Action<float> action)
		{
			_onFixedUpdateList.Add(new TickData
			{
				Id = ++_tickDataIdRef,
				Action = action,
				Subscriber = action.Target
			});
		}

		/// <inheritdoc />
		public void Unsubscribe(Action<float> action)
		{
			UnsubscribeOnUpdate(action);
			UnsubscribeOnFixedUpdate(action);
			UnsubscribeOnLateUpdate(action);
		}

		/// <inheritdoc />
		public void UnsubscribeOnUpdate(Action<float> action)
		{
			for (int i = 0; i < _onUpdateList.Count; i++)
			{
				if (_onUpdateList[i].Action == action && action.Target == _onUpdateList[i].Subscriber)
				{
					_onUpdateList.RemoveAt(i);
					return;
				}
			}
		}

		/// <inheritdoc />
		public void UnsubscribeOnFixedUpdate(Action<float> action)
		{
			for (int i = 0; i < _onFixedUpdateList.Count; i++)
			{
				if (_onFixedUpdateList[i].Action == action && action.Target == _onFixedUpdateList[i].Subscriber)
				{
					_onFixedUpdateList.RemoveAt(i);
					return;
				}
			}
		}

		/// <inheritdoc />
		public void UnsubscribeOnLateUpdate(Action<float> action)
		{
			for (int i = 0; i < _onLateUpdateList.Count; i++)
			{
				if (_onLateUpdateList[i].Action == action && action.Target == _onLateUpdateList[i].Subscriber)
				{
					_onLateUpdateList.RemoveAt(i);
					return;
				}
			}
		}

		/// <inheritdoc />
		public void UnsubscribeAllOnUpdate()
		{
			_onUpdateList.Clear();
		}

		/// <inheritdoc />
		public void UnsubscribeAllOnUpdate(object subscriber)
		{
			_onUpdateList.RemoveAll(data => data.Subscriber == subscriber);
		}

		/// <inheritdoc />
		public void UnsubscribeAllOnFixedUpdate()
		{
			_onFixedUpdateList.Clear();
		}

		/// <inheritdoc />
		public void UnsubscribeAllOnFixedUpdate(object subscriber)
		{
			_onFixedUpdateList.RemoveAll(data => data.Subscriber == subscriber);
		}

		/// <inheritdoc />
		public void UnsubscribeAllOnLateUpdate()
		{
			_onLateUpdateList.Clear();
		}

		/// <inheritdoc />
		public void UnsubscribeAllOnLateUpdate(object subscriber)
		{
			_onLateUpdateList.RemoveAll(data => data.Subscriber == subscriber);
		}

		/// <inheritdoc />
		public void UnsubscribeAll()
		{
			UnsubscribeAllOnUpdate();
			UnsubscribeAllOnFixedUpdate();
			UnsubscribeAllOnLateUpdate();
		}

		/// <inheritdoc />
		public void UnsubscribeAll(object subscriber)
		{
			UnsubscribeAllOnUpdate(subscriber);
			UnsubscribeAllOnFixedUpdate(subscriber);
			UnsubscribeAllOnLateUpdate(subscriber);
		}

		private void OnUpdate()
		{
			if (_onUpdateList.Count == 0)
			{
				return;
			}

			Update(_onUpdateList);
		}

		private void OnFixedUpdate()
		{
			if (_onFixedUpdateList.Count == 0)
			{
				return;
			}

			var arrayCopy = _onFixedUpdateList.ToArray();

			for (int i = 0; i < arrayCopy.Length; i++)
			{
				arrayCopy[i].Action(Time.fixedTime);
			}
		}

		private void OnLateUpdate()
		{
			if (_onLateUpdateList.Count == 0)
			{
				return;
			}

			Update(_onLateUpdateList);
		}

		private void Update(List<TickData> list)
		{
			if (list.Count == 0)
			{
				return;
			}

			var arrayCopy = list.ToArray();

			for (int i = 0; i < arrayCopy.Length; i++)
			{
				var tickData = arrayCopy[i];
				var time = tickData.RealTime ? Time.realtimeSinceStartup : Time.time;

				if (time < tickData.LastTickTime + tickData.DeltaTime)
				{
					continue;
				}

				var deltaTime = time - tickData.LastTickTime;
				var countBefore = list.Count;

				tickData.Action(deltaTime);

				// Check if the update was not unsubscribed in the call
				var index = i - (arrayCopy.Length - countBefore);
				if (list.Count > index && tickData == list[index])
				{
					var overFlow = tickData.DeltaTime == 0 ? 0 : deltaTime % tickData.DeltaTime;

					tickData.LastTickTime = tickData.TimeOverflowToNextTick ? time - overFlow : time;

					list[index] = tickData;
				}
			}
		}

		private struct TickData
		{
			public int Id;
			public Action<float> Action;
			public float DeltaTime;
			public bool TimeOverflowToNextTick;
			public bool RealTime;
			public float LastTickTime;
			public object Subscriber;

			public bool Equals(TickData other)
			{
				return other.Id == Id;
			}

			public override bool Equals(object other)
			{
				return other is TickData && Equals((TickData)other);
			}

			public override int GetHashCode()
			{
				return Id;
			}

			public static bool operator ==(TickData a, TickData b)
			{
				return a.Id == b.Id;
			}

			public static bool operator !=(TickData a, TickData b)
			{
				return a.Id != b.Id;
			}
		}
	}

	/// <summary>
	/// The MonoBehaviour class to be attached to the game object being processed in the <see cref="ITickService"/>
	/// This object will be the main invoker of all updates
	/// </summary>
	public class TickServiceMonoBehaviour : MonoBehaviour
	{
		public Action OnUpdate;
		public Action OnFixedUpdate;
		public Action OnLateUpdate;

		private void Update()
		{
			OnUpdate();
		}
		private void FixedUpdate()
		{
			OnFixedUpdate();
		}
		private void LateUpdate()
		{
			OnLateUpdate();
		}
	}
}