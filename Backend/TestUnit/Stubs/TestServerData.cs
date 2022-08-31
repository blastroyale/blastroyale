using System;
using FirstLight.Services;

// TODO: Implement properly, this is throw-away test code
public class ServerTestData : IDataProvider
{
	public bool TryGetData<T>(out T dat) where T : class
	{
		dat = (T) Activator.CreateInstance(typeof(T));
		return true;
	}

	public T GetData<T>() where T : class
	{
		var instance = (T) Activator.CreateInstance(typeof(T));
		return instance;
	}
}
