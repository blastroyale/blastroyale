using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Services;

namespace FirstLight.Server.SDK
{
	public class GameServerEvent
	{
		public string PlayerId;
		
		public GameServerEvent(string player)
		{
			PlayerId = player;
		}
	}

	/// <summary>
	/// Interface to register and callback events
	/// </summary>
	public interface IEventManager
	{
		/// <summary>
		/// Registers a given function to be called back when TEventType fires
		/// </summary>
		void RegisterEventListener<TEventType>(Func<TEventType, Task> listener) where TEventType : GameServerEvent;

		/// <summary>
		/// Calls a given event. All callbacks will be notified.
		/// </summary>
		Task CallEvent<TEventType>(TEventType ev);

		void RegisterCommandListener<TCommand>(Action<string, TCommand, ServerState> action)
			where TCommand : IGameCommand;

		void CallCommandEvent(string userId, IGameCommand command, ServerState finalState);
	}

	internal class Subscription
	{
		internal List<Listener> Listeners = new();
	}

	internal class Listener
	{
		internal object Action;
	}

	/// <summary>
	/// Minimal implementation of a plugin-based event manager.
	/// Will keep calbacks in-memory.
	/// </summary>
	public class PluginEventManager : IEventManager
	{
		private Dictionary<Type, Subscription> _listeners = new Dictionary<Type, Subscription>();
		private IPluginLogger _log;

		public PluginEventManager(IPluginLogger log)
		{
			_log = log;
		}

		public void RegisterEventListener<TEventType>(Func<TEventType, Task> listener) where TEventType : GameServerEvent
		{
			GetSubscribers(typeof(TEventType)).Listeners.Add(new Listener()
			{
				Action = listener
			});
		}

		public void RegisterCommandListener<TCommand>(Action<string, TCommand, ServerState> action)
			where TCommand : IGameCommand
		{
			var wrappedAction =
				new Action<string, IGameCommand, ServerState>((user, cmd, state) =>
					action.Invoke(user, (TCommand) cmd, state));
			var subs = GetSubscribers(typeof(TCommand));
			subs.Listeners.Add(new Listener()
			{
				Action = wrappedAction,
			});
		}

		public void CallCommandEvent(string userId, IGameCommand command, ServerState finalState)
		{
			var t = command.GetType();
			_log.LogDebug($"Calling command event {command.GetType().Name}");
			var subs = GetSubscribers(command.GetType());
			foreach (var listener in subs.Listeners)
			{
				var action = listener.Action as Action<string, IGameCommand, ServerState>;
				action.Invoke(userId, command, finalState);
			}
		}

		public async Task CallEvent<TEventType>(TEventType ev)
		{
			_log.LogDebug($"Calling event {ev.GetType().Name}");
			var subs = GetSubscribers(ev.GetType());
			foreach (var listener in subs.Listeners)
			{
				try
				{				
					var action = listener.Action as Func<TEventType, Task>;
					await action?.Invoke(ev);
				}
				catch (Exception e)
				{
					_log.LogError(e);
				}
			}
		}

		/// <summary>
		/// Gets all registered listeners for a given type.
		/// Will instantiate an empty list for the given type if needed.
		/// </summary>
		private Subscription GetSubscribers(Type t)
		{
			if (!_listeners.TryGetValue(t, out var subs))
			{
				subs = new Subscription();
				_listeners[t] = subs;
			}

			return subs;
		}
	}
}