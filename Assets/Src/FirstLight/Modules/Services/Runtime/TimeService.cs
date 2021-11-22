using System;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This service provides the time relative to the game's clock.
	/// The time provided is subjected of the manipulation from <see cref="ITimeManipulator"/>.
	/// </summary>
	public interface ITimeService
	{
		/// <summary>
		/// The current Gregorian UTC time relative to the game
		/// </summary>
		DateTime DateTimeUtcNow { get; }
		/// <summary>
		/// The current Unity's time since the game was started
		/// </summary>
		float UnityTimeNow { get; }
		/// <inheritdoc cref="Time.time"/>
		float UnityScaleTimeNow { get; }
		/// <summary>
		/// The current Unix time in milliseconds
		/// </summary>
		long UnixTimeNow { get; }
		/// <summary>
		/// Converts Gregorian UTC <paramref name="time"/> to unix time in milliseconds
		/// </summary>
		long UnixTimeFromDateTimeUtc(DateTime time);
		/// <summary>
		/// Converts Unity <paramref name="time"/> to unix time in milliseconds
		/// </summary>
		long UnixTimeFromUnityTime(float time);
		/// <summary>
		/// Converts Unix <paramref name="time"/> in milliseconds to Gregorian time in UTC
		/// </summary>
		DateTime DateTimeUtcFromUnixTime(long time);
		/// <summary>
		/// Converts Unity <paramref name="time"/> to Gregorian time in UTC
		/// </summary>
		DateTime DateTimeUtcFromUnityTime(float time);
		/// <summary>
		/// Converts Gregorian UTC <paramref name="time"/> to Unity time
		/// </summary>
		float UnityTimeFromDateTimeUtc(DateTime time);
		/// <summary>
		/// Converts Unix <paramref name="time"/> in milliseconds to Unity time
		/// </summary>
		float UnityTimeFromUnixTime(long time);
		/// <summary>
		/// Converts the given <paramref name="utcTime"/> to the based device local time (that can be manipulated by the player)
		/// </summary>
		DateTime ConvertToLocalTime(DateTime utcTime);
	}

	/// <inheritdoc cref="ITimeService"/>
	/// <remarks>
	/// Manipulates the service's time.
	/// This can be useful in cases where we want to speed up the game or to synchronize time with an outside source
	/// </remarks>
	public interface ITimeManipulator : ITimeService
	{
		/// <summary>
		/// Adds <paramref name="timeInSeconds"/> to the current game's clock.
		/// If positive speeds up time, if negative reverses time by the given amount in seconds
		/// </summary>
		void AddTime(float timeInSeconds);
		
		/// <summary>
		/// Synchronize the start game's time with the given <paramref name="initialTime"/>
		/// </summary>
		void SetInitialTime(DateTime initialTime);
	}

	/// <inheritdoc cref="ITimeService"/>
	public class TimeService : ITimeManipulator
	{
		private static readonly DateTime UnixInitialTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private float _initialUnityTime;
		private float _extraTime;
		private DateTime _initialTime = DateTime.MinValue;

		/// <inheritdoc />
		public DateTime DateTimeUtcNow => _initialTime.AddSeconds(Time.realtimeSinceStartup - _initialUnityTime).AddSeconds(_extraTime).ToUniversalTime();
		/// <inheritdoc />
		public float UnityTimeNow => Time.realtimeSinceStartup + _extraTime;
		/// <inheritdoc />
		public float UnityScaleTimeNow => Time.time + _extraTime;
		/// <inheritdoc />
		public long UnixTimeNow => (long)( DateTimeUtcNow - UnixInitialTime ).TotalMilliseconds;

		public TimeService()
		{
			_initialUnityTime = Time.realtimeSinceStartup;

			if (_initialTime == DateTime.MinValue)
			{
				_initialTime = DateTime.Now;
			}
		}

		/// <inheritdoc />
		public long UnixTimeFromDateTimeUtc(DateTime time)
		{
			return (long)( time.ToUniversalTime() - UnixInitialTime ).TotalMilliseconds;
		}

		/// <inheritdoc />
		public long UnixTimeFromUnityTime(float time)
		{
			return UnixTimeFromDateTimeUtc(DateTimeUtcFromUnityTime(time));
		}

		/// <inheritdoc />
		public DateTime DateTimeUtcFromUnixTime(long time)
		{
			return UnixInitialTime.AddMilliseconds(time).ToUniversalTime();
		}

		/// <inheritdoc />
		public DateTime DateTimeUtcFromUnityTime(float time)
		{
			return _initialTime.AddSeconds(time - _initialUnityTime).ToUniversalTime();
		}

		/// <inheritdoc />
		public float UnityTimeFromDateTimeUtc(DateTime time)
		{
			return (float) (time.ToUniversalTime() - _initialTime.ToUniversalTime()).TotalSeconds + _initialUnityTime;
		}

		/// <inheritdoc />
		public float UnityTimeFromUnixTime(long time)
		{
			return UnityTimeFromDateTimeUtc(DateTimeUtcFromUnixTime(time));
		}

		/// <inheritdoc />
		public DateTime ConvertToLocalTime(DateTime utcTime)
		{
			return DateTime.Now.AddTicks((utcTime - DateTimeUtcNow).Ticks);
		}

		/// <inheritdoc />
		public void AddTime(float timeInSeconds)
		{
			_extraTime += timeInSeconds;
		}

		/// <inheritdoc />
		public void SetInitialTime(DateTime initialTime)
		{
			_initialTime = initialTime;
			_initialUnityTime = Time.realtimeSinceStartup;
		}
	}
}