using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Server.SDK.Models;


namespace Backend.Game
{
	/// <summary>
    /// Server data provider. Wraps server data to be used in game logic.
    /// </summary>
    public class ServerPlayerDataProvider : IDataProvider
    {
    	private readonly ServerState _state;
    	
    	// This is needed because we need to provide instances of classes for client that are not needed for server.
    	// This should be removed when we remove this dependency from the server.
    	private readonly Dictionary<Type, object> _modelsConsumed = new()
    	{
    		{typeof(AppData), new AppData()}
    	};
    
    	// Types that are stored in the data provider but does not need to be updated server-side.
    	// Same as above, shall be removed as a depdendency soon.
    	private static readonly HashSet<Type> NotSaved = new(new[]
    	{
    		typeof(AppData)
    	});
    
    	public ServerPlayerDataProvider(ServerState state)
    	{
    		_state = state;
    	}
    
    	/// <summary>
    	/// Createa a new server state model with the updated models.
    	/// Will run trought all modified/read models by logic and serialize them back to server state.
    	/// </summary>
    	public ServerState GetUpdatedState()
    	{
    		var newState = new ServerState(_state);
    		foreach (var model in _modelsConsumed.Values)
    		{
    			if (NotSaved.Contains(model.GetType()))
    				continue;
    			
    			newState.UpdateModel(model); 
    		}
    		_modelsConsumed.Clear();
    		return newState;
    	}
    
    	/// <inheritdoc />
    	public bool TryGetData<T>(out T dat) where T : class
    	{
    		dat = GetData<T>();
    		return dat != null;
    	}
    
    	/// <inheritdoc />
    	public T GetData<T>() where T : class
    	{
    		// If we already have a cached version of that modified model, we use it
    		if (_modelsConsumed.TryGetValue(typeof(T), out var model))
    		{
    			return (T) model;
    		}
    	
    		// if not we try to deserialize the stored string
    		var data = _state.DeserializeModel<T>();
    		if (data == null)
    		{
    			throw new LogicException($"Trying to load model {typeof(T).FullName} from state, but only {string.Join(",",_state.Keys)} found");
    		}
    		_modelsConsumed[typeof(T)] = data;
    		return data; 
    	}
    }
}

