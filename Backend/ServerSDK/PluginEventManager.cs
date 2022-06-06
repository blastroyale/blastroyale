using Microsoft.Extensions.Logging;

namespace ServerSDK;

public class GameServerEvent { }

/// <summary>
/// Interface to register and callback events
/// </summary>
public interface IEventManager
{
	/// <summary>
	/// Registers a given function to be called back when TEventType fires
	/// </summary>
	void RegisterListener<TEventType>(Action<TEventType> listener) where TEventType : GameServerEvent;

	/// <summary>
	/// Calls a given event. All callbacks will be notified.
	/// </summary>
	void CallEvent<TEventType>(TEventType ev);
}


/// <summary>
/// Minimal implementation of a plugin-based event manager.
/// Will keep calbacks in-memory.
/// </summary>
public class PluginEventManager : IEventManager
{
	private Dictionary<Type, List<object>> _listeners = new();
	private ILogger _log;
	
	public PluginEventManager(ILogger log)
	{
		_log = log;
	}
	
	public void RegisterListener<TEventType>(Action<TEventType> listener) where TEventType : GameServerEvent
	{
		GetListeners(typeof(TEventType)).Add(listener);
	}

	public void CallEvent<TEventType>(TEventType ev)
	{
		_log.LogDebug($"Calling event {ev.GetType().Name}");
		foreach(var listener in GetListeners(typeof(TEventType)))
		{
			var action = listener as Action<TEventType>;
			action.Invoke(ev);
		}
	}

	/// <summary>
	/// Gets all registered listeners for a given type.
	/// Will instantiate an empty list for the given type if needed.
	/// </summary>
	private List<object> GetListeners(Type t)
	{
		if (!_listeners.TryGetValue(t, out var actionList))
		{
			actionList = new List<object>();
			_listeners[t] = actionList;
		}
		return actionList;
	}
}