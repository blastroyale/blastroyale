using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Quantum
{
	public static class ReplayJsonSerializerSettings
	{
		public static JsonSerializerSettings GetSettings()
		{
			return new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				SerializationBinder = new QuantumTypesBinder()
			};
		}
	}

	public class QuantumTypesBinder : ISerializationBinder
	{
		private Dictionary<string, Assembly> QuantumAssemblies = new Dictionary<string, Assembly>();

		public QuantumTypesBinder()
		{
			var assemblyPhotonDetermnistic = typeof(FP).Assembly;
			QuantumAssemblies.Add(assemblyPhotonDetermnistic.GetName().Name, assemblyPhotonDetermnistic);
			var assemblyQuantumCore = typeof(Quantum.Core.FrameBase).Assembly;
			QuantumAssemblies.Add(assemblyQuantumCore.GetName().Name, assemblyQuantumCore);
			var assemblyQuantumState = typeof(Quantum.Frame).Assembly;
			QuantumAssemblies.Add(assemblyQuantumState.GetName().Name, assemblyQuantumState);
			// add more for the plugin, and custom assemblies you might have
		}

		public Type BindToType(string assemblyName, string typeName)
		{
			// if from one of the known assemblies, force load from currently loaded version.
			if (QuantumAssemblies.ContainsKey(assemblyName))
			{
				return QuantumAssemblies[assemblyName].GetType(typeName);
			} else
			{
				return Type.GetType(typeName);
			}
		}

		public void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			assemblyName = null;
			typeName = serializedType.Name;
		}
	}
}