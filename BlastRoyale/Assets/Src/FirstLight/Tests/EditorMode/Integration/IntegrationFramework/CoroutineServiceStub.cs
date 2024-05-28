using System;
using System.Collections;
using FirstLight.Services;
using UnityEditor.VersionControl;
using UnityEngine;

namespace FirstLight.Tests.EditorMode
{
	public class CoroutineStub : IAsyncCoroutine {
		private bool _isCompleted;
		private Coroutine _coroutine;

		public bool IsCompleted => _isCompleted;

		public Coroutine Coroutine => _coroutine;

		public void OnComplete(Action onComplete)
		{
		
		}
	}
	
	public class StubCoroutineService: ICoroutineService
	{
		public void Dispose()
		{
			
		}

		public Coroutine StartCoroutine(IEnumerator routine)
		{
			return null;
		}

		public IAsyncCoroutine StartAsyncCoroutine(IEnumerator routine)
		{
			return new CoroutineStub();
		}

		public IAsyncCoroutine<T> StartAsyncCoroutine<T>(IEnumerator routine)
		{
			return null;
		}

		public void StopCoroutine(Coroutine coroutine)
		{
			
		}

		public void StopAllCoroutines()
		{
			
		}
	}
}