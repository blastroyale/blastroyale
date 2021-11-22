using System;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This class acts as an helper for converting references between quantum and the frontend
	/// </summary>
	public static class QuantumConverter
	{
		/// <inheritdoc cref="QuantumPath(string)"/>
		/// <remarks>
		/// Use this for getting the asset path for <see cref="EntityView"/>
		/// </remarks>
		public static string QuantumPathView(string path)
		{
			return QuantumPath(path) + "|EntityView";
		}

		/// <inheritdoc cref="QuantumPath(string)"/>
		/// <remarks>
		/// Use this for getting the asset path for <see cref="EntityPrototype"/>
		/// </remarks>
		public static string QuantumPathPrototype(string path)
		{
			return QuantumPath(path) + "|EntityPrototype";
		}
		
		/// <summary>
		/// This extension helps to get the quantum's asset path of the given generic path for the Quantum's
		/// <see cref="UnityDB"/> asset loading
		/// </summary>
		public static string QuantumPath(string path)
		{
			if (path.StartsWith("Assets/"))
			{
				path = path.Substring("Assets/".Length);
			}

			var index = path.LastIndexOf('.');

			return index < 0 ? path : path.Substring(0, index);
		}

		/// <summary>
		/// Requests the <see cref="AssetRefEntityPrototype"/> defined in the given project path
		/// </summary>
		/// <remarks>
		/// Avoid doing this call at runtime because might be an expensive operation
		/// </remarks>
		public static AssetRefEntityPrototype QuantumEntityRef(string path)
		{
			if (UnityDB.DefaultResourceManager.TryGetAssetResource(QuantumPathPrototype(path), out var resource))
			{
				return new AssetRefEntityPrototype { Id = resource.Guid };
			}

			throw new ArgumentException($"There is no Quantum asset configured for in the path: {path}");
		}
	}
}