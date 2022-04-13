using System;
using System.Collections.Generic;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;

namespace Backend.Game;

/// <summary>
/// Server data provider. Wraps server data to be used in game logic.
/// </summary>
public class ServerPlayerDataProvider : IDataProvider
{
	private readonly ServerData _data;
	
	public ServerPlayerDataProvider(ServerData data)
	{
		_data = data;
	}

	/// <inheritdoc />
	public bool TryGetData<T>(out T dat) where T : class
	{
		dat = _data.GetModel<T>();
		return dat != null;
	}

	/// <inheritdoc />
	public T GetData<T>() where T : class
	{
		return _data.GetModel<T>(); // TODO: Cache deserialized model.
	}
}