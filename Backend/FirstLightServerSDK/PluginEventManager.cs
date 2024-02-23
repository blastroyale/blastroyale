using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Modules;


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

	public static class EventPriority
	{
		// Executes first
		public const int FIRST = 100;
		public const int MIDDLE = 50;
		public const int LAST = 0;
		// Executes last
	}


	/// <summary>
	/// Interface to register and callback events
	/// </summary>
	public interface IEventManager
	{
		/// <summary>
		/// Registers a given function to be called back when TEventType fires
		/// </summary>
		void RegisterEventListener<TEventType>(Func<TEventType, Task> listener, int priority = EventPriority.MIDDLE) where TEventType : GameServerEvent;

		/// <summary>
		/// Calls a given event. All callbacks will be notified.
		/// </summary>
		Task CallEvent<TEventType>(TEventType ev);

		void RegisterCommandListener<TCommand>(Func<string, TCommand, ServerState, Task> action)
			where TCommand : IGameCommand;

		Task CallCommandEvent(string userId, IGameCommand command, ServerState finalState);
	}

	internal class Subscription
	{
		internal List<Listener> Listeners = new();
	}

	internal class Listener
	{
		internal object Action;
		internal int Priority;
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

		public void RegisterEventListener<TEventType>(Func<TEventType, Task> listener, int eventPriority = EventPriority.MIDDLE) where TEventType : GameServerEvent
		{
			var listeners = GetSubscribers(typeof(TEventType)).Listeners;
			listeners.Add(new Listener()
			{
				Action = listener,
				Priority = eventPriority
			});
			listeners.Sort((listener1, listener2) => listener2.Priority - listener1.Priority);
		}

		public void RegisterCommandListener<TCommand>(Func<string, TCommand, ServerState, Task> action)
			where TCommand : IGameCommand
		{
			var wrappedAction =
				new Func<string, IGameCommand, ServerState, Task>((user, cmd, state) =>
					action.Invoke(user, (TCommand) cmd, state));
			var subs = GetSubscribers(typeof(TCommand));
			subs.Listeners.Add(new Listener()
			{
				Action = wrappedAction,
			});
		}

		public async Task CallCommandEvent(string userId, IGameCommand command, ServerState finalState)
		{
			var t = command.GetType();
			_log.LogDebug($"Calling command execution finish for plugins: {command.GetType().Name}");
			var subs = GetSubscribers(command.GetType());
			foreach (var listener in subs.Listeners)
			{
				var action = listener.Action as Func<string, IGameCommand, ServerState, Task>;
				await action!.Invoke(userId, command, finalState);
			}
		}

		public async Task CallEvent<TEventType>(TEventType ev)
		{
			DebugEvent(ev);
			var subs = GetSubscribers(ev.GetType());
			foreach (var listener in subs.Listeners)
			{
				try
				{
					var action = listener.Action as Func<TEventType, Task>;
					await action!.Invoke(ev);
				}
				catch (Exception e)
				{
					_log.LogError(e);
					throw;
				}
			}
		}
		
		[Conditional("DEBUG")]
		private void DebugEvent(object ev)
		{
			_log.LogInformation($"Calling event for plugins: {ev.GetType().GetRealTypeName()}");
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