using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This interface acts as a wait for the completion of the coroutine.
	/// It allows to passive wait for the completion of a coroutine to invoke a callback when finished.
	/// </summary>
	public interface IAsyncCoroutine
	{
		/// <summary>
		/// Get the complete operation status of the coroutine
		/// </summary>
		bool IsCompleted { get; }
		/// <summary>
		/// Get the current coroutine being executed
		/// </summary>
		Coroutine Coroutine { get; }
		
		/// <summary>
		/// Sets the action <paramref name="onComplete"/> callback to be invoked when the coroutine is completed
		/// </summary>
		void OnComplete(Action onComplete);
	}

	/// <inheritdoc cref="IAsyncCoroutine"/>
	public interface IAsyncCoroutine<T>
	{
		/// <inheritdoc cref="IAsyncCoroutine.IsCompleted"/>
		bool IsCompleted { get; }
		/// <inheritdoc cref="IAsyncCoroutine.Coroutine"/>
		Coroutine Coroutine { get; }
		/// <summary>
		/// The data to be returned on the coroutine completion
		/// </summary>
		T Data { get; }
		
		/// <summary>
		/// Sets the action <paramref name="onComplete"/> callback to be invoked when the coroutine is completed with the
		/// <paramref name="data"/> reference in the callback
		/// </summary>
		void OnComplete(T data, Action<T> onComplete);
	}
	
	/// <summary>
	/// The coroutine service allows the use of coroutines outside of the scope of Unity's game objects.
	/// This allows to get the power of coroutines in pure C# classes.
	/// It also extends the functionality of coroutines by providing the use of <see cref="IAsyncCoroutine"/> to
	/// have a callback on complete of the coroutine.
	/// The Coroutine Service creates a game object in the scene that will be the true container of all coroutines
	/// to be created in through this service.
	/// The coroutine service will keep the coroutines on scene load/unload.
	/// </summary>
	public interface ICoroutineService : IDisposable
	{
		/// <summary>
		/// Follows the same principle and execution of <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> but returns
		/// and can be used in pure C# classes. It can be helpful also to enable coroutines to execute when an object is disabled
		/// </summary>
		Coroutine StartCoroutine(IEnumerator routine);
		/// <summary>
		/// Follows the same principle and execution of <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> but returns
		/// a <see cref="IAsyncCoroutine"/> to provide a callback on complete of the coroutine
		/// </summary>
		IAsyncCoroutine StartAsyncCoroutine(IEnumerator routine);
		/// <summary>
		/// Follows the same principle and execution of <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> but returns
		/// a <see cref="IAsyncCoroutine{T}"/> to provide a callback on complete of the coroutine with given <typeparamref name="T"/> type
		/// </summary>
		IAsyncCoroutine<T> StartAsyncCoroutine<T>(IEnumerator routine);
		/// <inheritdoc cref="MonoBehaviour.StopCoroutine(Coroutine)"/>
		void StopCoroutine(Coroutine coroutine);
		/// <inheritdoc cref="MonoBehaviour.StopAllCoroutines"/>
		void StopAllCoroutines();
	}

	/// <inheritdoc cref="ICoroutineService"/>
	public class CoroutineService : ICoroutineService
	{
		private CoroutineServiceMonoBehaviour _serviceObject;

		public CoroutineService()
		{
			var gameObject = new GameObject(nameof(CoroutineServiceMonoBehaviour));

			_serviceObject = gameObject.AddComponent<CoroutineServiceMonoBehaviour>();
			
			Object.DontDestroyOnLoad(gameObject);
		}

		/// <summary>
		/// Cleans the coroutine service and deletes the coroutine game object that contains all the coroutines in the game.
		/// This will also stop all currently running coroutines.
		/// </summary>
		public void Dispose()
		{
			if(_serviceObject == null)
			{
				return;
			}
			
			_serviceObject.StopAllCoroutines();

			Object.Destroy(_serviceObject.gameObject);

			_serviceObject = null;
		}

		/// <inheritdoc />
		public Coroutine StartCoroutine(IEnumerator routine)
		{
			return _serviceObject.ExternalStartCoroutine(routine);
		}

		/// <inheritdoc />
		public IAsyncCoroutine StartAsyncCoroutine(IEnumerator routine)
		{
			var asyncCoroutine = new AsyncCoroutine();

			asyncCoroutine.SetCoroutine(_serviceObject.ExternalStartCoroutine(InternalCoroutine(routine, asyncCoroutine)));

			return asyncCoroutine;
		}

		/// <inheritdoc />
		public IAsyncCoroutine<T> StartAsyncCoroutine<T>(IEnumerator routine)
		{
			var asyncCoroutine = new AsyncCoroutine<T>();

			asyncCoroutine.SetCoroutine(_serviceObject.ExternalStartCoroutine(InternalCoroutine(routine, asyncCoroutine)));

			return asyncCoroutine;
		}
		
		/// <inheritdoc />
		public void StopCoroutine(Coroutine coroutine)
		{
			if (coroutine == null || _serviceObject == null || _serviceObject.gameObject == null)
			{
				return;
			}
			
			_serviceObject.ExternalStopCoroutine(coroutine);
		}

		/// <inheritdoc />
		public void StopAllCoroutines()
		{
			if (_serviceObject == null || _serviceObject.gameObject == null)
			{
				return;
			}
			
			_serviceObject.StopAllCoroutines();
		}

		private static IEnumerator InternalCoroutine(IEnumerator routine, ICompleteCoroutine completed)
		{
			yield return routine;

			completed.Completed();
		}
		
		#region Private Interfaces
		
		private interface ICompleteCoroutine
		{
			void Completed();
			
			void SetCoroutine(Coroutine coroutine);
		}
		
		private class AsyncCoroutine : IAsyncCoroutine, ICompleteCoroutine
		{
			private Action _onComplete;
		
			public bool IsCompleted { get; private set; }
			public Coroutine Coroutine { get; private set; }

			public void SetCoroutine(Coroutine coroutine)
			{
				Coroutine = coroutine;
			}
		
			public void OnComplete(Action onComplete)
			{
				_onComplete = onComplete;
			}

			public void Completed()
			{
				if (IsCompleted)
				{
					return;
				}

				IsCompleted = true;
				Coroutine = null;
			
				_onComplete?.Invoke();
			}
		}

		private class AsyncCoroutine<T> : IAsyncCoroutine<T>, ICompleteCoroutine
		{
			private Action<T> _onComplete;
		
			public bool IsCompleted { get; private set; }
			public Coroutine Coroutine { get; private set; }
			public T Data { get; private set; }

			public void SetCoroutine(Coroutine coroutine)
			{
				Coroutine = coroutine;
			}
		
			public void OnComplete(T data, Action<T> onComplete)
			{
				_onComplete = onComplete;
				
				Data = data;
			}

			public void Completed()
			{
				if (IsCompleted)
				{
					return;
				}

				IsCompleted = true;
				Coroutine = null;
			
				_onComplete?.Invoke(Data);
			}
		}
		
		#endregion
	}

	/// <summary>
	/// MonoBehaviour to be attached to the game object in the scene being created in the <see cref="ICoroutineService"/>.
	/// This will be the container of all the coroutines created by the coroutine service.
	/// </summary>
	public class CoroutineServiceMonoBehaviour : MonoBehaviour
	{
		/// <inheritdoc cref="ICoroutineService.StartCoroutine(IEnumerator)"/>
		public Coroutine ExternalStartCoroutine(IEnumerator routine)
		{
			return StartCoroutine(routine);
		}

		/// <inheritdoc cref="ICoroutineService.StopCoroutine(Coroutine)"/>
		public void ExternalStopCoroutine(Coroutine coroutine)
		{
			StopCoroutine(coroutine);
		}
	}
}