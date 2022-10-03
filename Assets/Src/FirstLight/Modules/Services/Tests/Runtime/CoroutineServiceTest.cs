using System.Collections;
using FirstLight.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	[RequiresPlayMode]
	public class CoroutineServiceTest
	{
		private CoroutineService _coroutineService;
		private int _testValue;

		private IEnumerator TestCoroutine(int value)
		{
			yield return null;

			_testValue = value;
		}

		[SetUp]
		public void Init()
		{
			_coroutineService = new CoroutineService();
			_testValue = 0;
		}

		[TearDown]
		public void Dispose()
		{
			_coroutineService.Dispose();
		}
		
		[UnityTest]
		public IEnumerator StartCoroutine_Successfully()
		{
			const int testValue1 = 5;

			yield return _coroutineService.StartCoroutine(TestCoroutine(testValue1));
			
			Assert.AreEqual(testValue1, _testValue); 
		}
		
		[UnityTest]
		public IEnumerator StartAsyncCoroutine_Successfully()
		{
			const int testValue1 = 5;
			const int testValue2 = 10;
			int testCompleted = 0;

			IAsyncCoroutine asyncCoroutine = _coroutineService.StartAsyncCoroutine(TestCoroutine(testValue1));
			asyncCoroutine.OnComplete(() => testCompleted = testValue2);

			yield return asyncCoroutine.Coroutine;
			
			Assert.IsTrue(asyncCoroutine.IsCompleted);
			Assert.AreEqual(testValue1, _testValue); 
			Assert.AreEqual(testValue2, testCompleted); 
		}
		
		[UnityTest]
		public IEnumerator StartAsyncCoroutine_WithData_Successfully()
		{
			const int testValue1 = 5;
			const int testValue2 = 10;
			int testCompleted = 0;

			var asyncCoroutine = _coroutineService.StartAsyncCoroutine<int>(TestCoroutine(testValue1));
			asyncCoroutine.OnComplete(testValue2, newValue => testCompleted = newValue);

			yield return asyncCoroutine.Coroutine;
			
			Assert.IsTrue(asyncCoroutine.IsCompleted);
			Assert.AreEqual(testValue1, _testValue); 
			Assert.AreEqual(testValue2, testCompleted); 
		}
		
		[UnityTest]
		public IEnumerator StopCoroutine_Successfully()
		{
			const int testValue1 = 5;

			var coroutine = _coroutineService.StartCoroutine(TestCoroutine(testValue1));
			_coroutineService.StopCoroutine(coroutine);
			
			Assert.AreNotEqual(testValue1, _testValue); 

			yield return new WaitForSeconds(0.1f);
			
			Assert.AreNotEqual(testValue1, _testValue); 
		}
		
		[UnityTest]
		public IEnumerator StopAsyncCoroutine_Successfully()
		{
			const int testValue1 = 5;
			const int testValue2 = 10;
			int testCompleted = 0;

			IAsyncCoroutine asyncCoroutine = _coroutineService.StartAsyncCoroutine(TestCoroutine(testValue1));
			asyncCoroutine.OnComplete(() => testCompleted = testValue2);
			
			_coroutineService.StopCoroutine(asyncCoroutine.Coroutine);
			
			Assert.False(asyncCoroutine.IsCompleted);
			Assert.AreNotEqual(testValue1, _testValue); 
			Assert.AreNotEqual(testValue2, testCompleted); 

			yield return new WaitForSeconds(0.1f);
			
			Assert.False(asyncCoroutine.IsCompleted);
			Assert.AreNotEqual(testValue1, _testValue); 
			Assert.AreNotEqual(testValue2, testCompleted); 
		}
		
		[UnityTest]
		public IEnumerator StopAllCoroutines_Successfully()
		{
			const int testValue1 = 5;
			const int testValue2 = 10;
			const int testValue3 = 20;
			int testCompleted = 0;

			IAsyncCoroutine asyncCoroutine = _coroutineService.StartAsyncCoroutine(TestCoroutine(testValue1));
			asyncCoroutine.OnComplete(() => testCompleted = testValue2);
			_coroutineService.StartCoroutine(TestCoroutine(testValue3));
			
			_coroutineService.StopAllCoroutines();
			
			Assert.False(asyncCoroutine.IsCompleted);
			Assert.AreNotEqual(testValue1, _testValue); 
			Assert.AreNotEqual(testValue2, testCompleted); 
			Assert.AreNotEqual(testValue3, _testValue); 

			yield return new WaitForSeconds(0.1f);
			
			Assert.False(asyncCoroutine.IsCompleted);
			Assert.AreNotEqual(testValue1, _testValue); 
			Assert.AreNotEqual(testValue2, testCompleted); 
			Assert.AreNotEqual(testValue3, _testValue); 
		}
	}
}