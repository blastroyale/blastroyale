using System;
using FirstLight.Services;

namespace Tests.Stubs;

/// <summary>
/// Server test time for testing purposes.
/// Starts couting the time when tests instantiates the class.
/// </summary>
public class ServerTestTime: ITimeService
{
	private static readonly DateTime UnixInitialTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	private float _startTime;
	private float _extraTime;
	private DateTime _initialTime = DateTime.MinValue;

	/// <inheritdoc />
	public DateTime DateTimeUtcNow => DateTime.UtcNow;
	/// <inheritdoc />
	public float UnityTimeNow => _extraTime;
	/// <inheritdoc />
	public float UnityScaleTimeNow => _extraTime;
	/// <inheritdoc />
	public long UnixTimeNow => (long)( DateTimeUtcNow - UnixInitialTime ).TotalMilliseconds;

	public ServerTestTime()
	{
		_initialTime = DateTime.Now;
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
		return _initialTime.AddSeconds(time - _startTime).ToUniversalTime();
	}

	/// <inheritdoc />
	public float UnityTimeFromDateTimeUtc(DateTime time)
	{
		return (float) (time.ToUniversalTime() - _initialTime.ToUniversalTime()).TotalSeconds + _startTime;
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
	}
}