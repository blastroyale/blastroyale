using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Server.SDK.Modules;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Represents stored server data.
	/// Stores { type names : serialized type } in its internal data. 
	/// </summary>
	public class ServerState : Dictionary<string, string>
	{
		private StateDelta _delta = new StateDelta();
	
		public ServerState()
		{
		}

		public ServerState(Dictionary<string, string> data) : base(data)
		{
		}

		public bool HasDelta() => _delta.GetModifiedTypes().Count() > 0;
		
		public StateDelta GetDeltas() => _delta;
		
		public ulong GetVersion()
		{
			if (!TryGetValue("version", out var versionString))
			{
				versionString = "1";
			}

			return ulong.Parse(versionString);
		}

		public void SetVersion(ulong version)
		{
			this["version"] = version.ToString();
		}

		/// <summary>
		/// Sets a given model in server state.
		/// Will serialize the model.
		/// </summary>
		public void UpdateModel(object model)
		{
			var serialized = ModelSerializer.Serialize(model);
			var typeName = serialized.Key;
			var data = serialized.Value;
			if (!TryGetValue(typeName, out var oldData) || oldData != data)
			{
				_delta.TrackModification(model);
			}
			this[typeName] = data;
		}


		/// <summary>
		/// Obtains a serialized model inside server's data.
		/// </summary>
		public object DeserializeModel(Type type)
		{
			return TryGetValue(type.FullName, out var data)
					   ? ModelSerializer.Deserialize(type, data)
					   : Activator.CreateInstance(type);
		}

		/// <summary>
		/// Gets the server state in raw json string format
		/// </summary>
		public string GetRawJson<T>()
		{
			return this[typeof(T).FullName];
		}

		/// <summary>
		/// Checks if there's specific model in server state
		/// </summary>
		public bool Has<T>()
		{
			return ContainsKey(typeof(T).FullName);
		}
		
		/// <summary>
		/// Generic wrapper of <see cref="DeserializeModel"/>
		/// </summary>
		public T DeserializeModel<T>()
		{
			return (T)DeserializeModel(typeof(T));
		}

		/// <summary>
		/// Updates the current delta values
		/// </summary>
		/// <param name="delta"></param>
		public void SetDelta(StateDelta delta)
		{
			this._delta = delta;
		}
		
		/// <summary>
		/// Obtains a server state that only contains keys that were updated after
		/// the class instantiation. This is a way to optimize which keys are sent to external
		/// providers (e.g Playfab)
		/// </summary>
		public ServerState GetOnlyUpdatedState()
		{
			var newStateToUpdate = new ServerState();
			var updatedTypes = _delta.GetModifiedTypes();
			if (!updatedTypes.Any())
			{
				return newStateToUpdate;
			}
			
			foreach (var updatedType in updatedTypes)
			{
				if (TryGetValue(updatedType.FullName, out var oldData))
				{
					newStateToUpdate[updatedType.FullName] = oldData;
				}
			}
			return newStateToUpdate;
		}
	}
}