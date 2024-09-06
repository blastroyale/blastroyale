using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Models;
using FirstLight.Services;

// TODO: Implement properly, this is throw-away test code
public class ServerTestData : IDataProvider
{
	public bool TryGetData<T>(out T dat) where T : class
	{
		dat = (T) Activator.CreateInstance(typeof(T));
		return true;
	}

	public bool TryGetData(Type type, out object dat)
	{
		dat = Activator.CreateInstance(type);
		return true;
	}

	public T GetData<T>() where T : class
	{
		var instance = (T) Activator.CreateInstance(typeof(T));
		return instance;
	}

	public object GetData(Type type)
	{
		return Activator.CreateInstance(type)!;
	}

	public IEnumerable<Type> GetKeys()
	{
		throw new NotImplementedException();
	}
	
}
