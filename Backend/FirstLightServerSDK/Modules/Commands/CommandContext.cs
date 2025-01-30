using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Server.SDK.Modules.Commands
{
	/// <summary>
	/// Rudimentary and minimal object container
	/// </summary>
	public class ObjectContainer
	{
		private Dictionary<Type, object> _objects = new();

		public void Add<T>(T o)
		{
			_objects[typeof(T)] = o;
		}

		public T? Get<T>() where T : class
		{
			return _objects[typeof(T)] as T;
		}
	}

	/// <summary>
	/// Type holder to reference all game logic objects
	/// </summary>
	public class LogicContainer : ObjectContainer
	{
	}

	/// <summary>
	/// Type holder to reference all game service objects
	/// </summary>
	public class ServiceContainer : ObjectContainer
	{
	}

	/// <summary>
	/// Represents the context of a command execution on a given time
	/// </summary>
	public class CommandExecutionContext
	{
		public readonly LogicContainer Logic;
		public readonly ServiceContainer Services;
		public readonly IDataProvider Data;

		public CommandExecutionContext(LogicContainer logic, ServiceContainer services, IDataProvider data)
		{
			Logic = logic;
			Services = services;
			Data = data;
		}
	}
}