using System.Collections.Generic;
using System.Threading;

namespace Scripts.Base;

public class ConfigRegistry
{
	private static readonly Dictionary<string, string> _configValues = new();
	private static readonly ReaderWriterLockSlim _lock = new();

	public static void Set(string key, string value)
	{
		_lock.EnterWriteLock();
		try
		{
			_configValues[key] = value;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	public static string Get(string key)
	{
		_lock.EnterReadLock();
		try
		{
			if (_configValues.TryGetValue(key, out string value))
			{
				return value;
			}
			return null; // Or throw an exception if you prefer
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}
}