using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Represents stored server data.
	/// Stores { type names : serialized type } in its internal data. 
	/// </summary>
	public class ServerState : Dictionary<string, string>
	{
		private HashSet<Type> _updatedTypes = new HashSet<Type>();

		public ServerState()
		{
		}

		public ServerState(Dictionary<string, string> data) : base(data)
		{
		}

		public HashSet<Type> UpdatedTypes => _updatedTypes;

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
			if (!this.TryGetValue(typeName, out var oldData) || oldData != data)
			{
				_updatedTypes.Add(model.GetType());
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
		/// Generic wrapper of <see cref="DeserializeModel"/>
		/// </summary>
		public T DeserializeModel<T>()
		{
			return (T)DeserializeModel(typeof(T));
		}


		public ServerState GetOnlyUpdatedState()
		{
			if (_updatedTypes.Count == 0)
			{
				return this;
			}
			
			var newStateToUpdate = new ServerState();
			foreach (var updatedType in _updatedTypes)
			{
				if (this.TryGetValue(updatedType.FullName, out var oldData))
				{
					if (oldData == null)
					{
						// log no exp to just track
						Console.WriteLine($"[Call Gabriel] Updated data for model {updatedType.Name} is null");
					}

					newStateToUpdate[updatedType.FullName] = oldData;
				}
			}

			return newStateToUpdate;
		}
	}
}