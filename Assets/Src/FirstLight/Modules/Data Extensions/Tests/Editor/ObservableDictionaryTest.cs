using System.Collections.Generic;
using FirstLight;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableDictionaryTest
	{
		private const int _key = 0;
		private const int _previousValue = 5;
		private const int _newValue = 10;
		
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller<in TKey, in TValue>
		{
			void Call(TKey key, TValue previousValue, TValue newValue, ObservableUpdateType updateType);
		}
		
		private ObservableDictionary<int, int> _observableDictionary;
		private ObservableResolverDictionary<int, int> _observableResolverDictionary;
		private IDictionary<int,int> _mockDictionary;
		private IMockCaller<int, int> _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller<int, int>>();
			_mockDictionary = Substitute.For<IDictionary<int, int>>();
			_observableDictionary = new ObservableDictionary<int, int>(_mockDictionary);
			_observableResolverDictionary = new ObservableResolverDictionary<int, int>(() => _mockDictionary);

			_mockDictionary.TryGetValue(_key, out _).Returns(callInfo =>
			{
				callInfo[1] = _mockDictionary[_key];
				return true;
			});
		}

		[Test]
		public void ValueCheck()
		{
			_mockDictionary[_key].Returns(_previousValue);
			
			Assert.AreEqual(_previousValue, _observableDictionary[_key]);
			Assert.AreEqual(_previousValue, _observableResolverDictionary[_key]);
		}

		[Test]
		public void ValueSetCheck()
		{
			const int valueCheck1 = 5;
			const int valueCheck2 = 6;
			const int valueCheck3 = 7;
			
			_mockDictionary[_key] = valueCheck1;
			
			Assert.AreEqual(valueCheck1, _observableDictionary[_key]);
			Assert.AreEqual(valueCheck1, _observableResolverDictionary[_key]);

			_observableDictionary[_key] = valueCheck2;
			
			Assert.AreEqual(valueCheck2, _observableDictionary[_key]);
			Assert.AreEqual(valueCheck2, _observableResolverDictionary[_key]);

			_observableResolverDictionary[_key] = valueCheck3;
			
			Assert.AreEqual(valueCheck3, _observableDictionary[_key]);
			Assert.AreEqual(valueCheck3, _observableResolverDictionary[_key]);
		}

		[Test]
		public void ObserveCheck()
		{
			_observableDictionary.Observe(_key, _caller.Call);
			_observableDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_key, _caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			
			// _caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
			
			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.Received(4).Call(_key, _previousValue, _newValue, ObservableUpdateType.Added);
			_caller.Received(4).Call(_key, _previousValue, _newValue,ObservableUpdateType.Updated);
			_caller.Received(4).Call(_key, _previousValue, _newValue, ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeObserveCheck()
		{
			_observableDictionary.InvokeObserve(_key, _caller.Call);
			_observableResolverDictionary.InvokeObserve(_key, _caller.Call);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received(2).Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck()
		{
			_observableDictionary.Observe(_key, _caller.Call);
			_observableDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_key, _caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			
			_observableDictionary.InvokeUpdate(_key);
			_observableResolverDictionary.InvokeUpdate(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received(4).Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(),Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck_NotObserving_DoesNothing()
		{
			_observableDictionary.InvokeUpdate(_key);
			_observableResolverDictionary.InvokeUpdate(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(),Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObserveCheck()
		{
			_observableDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			_observableDictionary.StopObserving(_caller.Call);
			_observableResolverDictionary.StopObserving(_caller.Call);

			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObserve_KeyCheck()
		{
			_observableDictionary.Observe(_key, _caller.Call);
			_observableResolverDictionary.Observe(_key, _caller.Call);
			_observableDictionary.StopObserving(_key);
			_observableResolverDictionary.StopObserving(_key);

			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAllCheck()
		{
			_observableDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			_observableDictionary.StopObservingAll(_caller);
			_observableResolverDictionary.StopObservingAll(_caller);

			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_MultipleCalls_Check()
		{
			_observableDictionary.Observe(_caller.Call);
			_observableDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			_observableDictionary.StopObservingAll(_caller);
			_observableResolverDictionary.StopObservingAll(_caller);

			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_Everything_Check()
		{
			_observableDictionary.Observe(_caller.Call);
			_observableResolverDictionary.Observe(_caller.Call);
			_observableDictionary.StopObservingAll();
			_observableResolverDictionary.StopObservingAll();

			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_NotObserving_DoesNothing()
		{
			_observableDictionary.StopObservingAll(_caller);
			_observableResolverDictionary.StopObservingAll(_caller);

			_observableDictionary.Add(_key, _previousValue);
			_observableResolverDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_observableResolverDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_observableResolverDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),Arg.Any<ObservableUpdateType>());
		}
	}
}