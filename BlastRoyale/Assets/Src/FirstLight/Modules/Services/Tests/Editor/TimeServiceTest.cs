using System;
using FirstLight.Services;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	[TestFixture]
	public class TimeServiceTest
	{
		private const float _errorValue = 0.01f;
		private TimeService _timeService;

		[SetUp]
		public void Init()
		{
			_timeService = new TimeService();
		}

		[Test]
		public void DateTime_Convertions_Successfully()
		{
			Assert.GreaterOrEqual(_errorValue, (_timeService.DateTimeUtcFromUnityTime(_timeService.UnityTimeNow) - _timeService.DateTimeUtcNow).TotalMilliseconds);
			Assert.GreaterOrEqual(_errorValue, (_timeService.DateTimeUtcFromUnixTime(_timeService.UnixTimeNow) - _timeService.DateTimeUtcNow).TotalMilliseconds);
		}

		[Test]
		public void UnityTime_Convertions_Successfully()
		{
			Assert.GreaterOrEqual(_errorValue, _timeService.UnityTimeFromDateTimeUtc(_timeService.DateTimeUtcNow) - _timeService.UnityTimeNow);
			Assert.GreaterOrEqual(_errorValue, _timeService.UnityTimeFromUnixTime(_timeService.UnixTimeNow) - _timeService.UnityTimeNow);
		}

		[Test]
		public void UnixTime_Convertions_Successfully()
		{
			Assert.GreaterOrEqual(_errorValue, _timeService.UnixTimeFromDateTimeUtc(_timeService.DateTimeUtcNow) - _timeService.UnixTimeNow);
			Assert.GreaterOrEqual(_errorValue, _timeService.UnixTimeFromUnityTime(_timeService.UnityTimeNow) - _timeService.UnixTimeNow);
		}

		[Test]
		public void AddTime_AllTimeTypes_Successfully()
		{
			var extraTime = 50.5f;
			var extraTimeInMilliseconds = TimeSpan.FromSeconds(extraTime).TotalMilliseconds;
			var dateTime = _timeService.DateTimeUtcNow;
			var unityTime = _timeService.UnityTimeNow;
			var unixTime = _timeService.UnixTimeNow;

			_timeService.AddTime(extraTime);

			Assert.LessOrEqual(0, _timeService.DateTimeUtcNow.CompareTo(dateTime.AddSeconds(extraTime)));
			Assert.GreaterOrEqual(_timeService.UnityTimeNow, unityTime + extraTime);
			Assert.GreaterOrEqual(_timeService.UnixTimeNow, unixTime - extraTimeInMilliseconds);
		}
	}
}
