using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This service allows to manage multiple <see cref="Material"/> defined by <typeparamref name="T"/> vfx id enum type.
	/// </summary>
	public interface IMaterialVfxService<T> : IDisposable where T : struct, Enum
	{
		/// <summary>
		/// Requests a new <see cref="Material"/> defined by the given <paramref name="id"/>
		/// </summary>
		Material Get(T id);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Used only on internal creation data and should not be exposed to the views
	/// </remarks>
	public interface IMaterialVfxInternalService<T> : IMaterialVfxService<T> where T : struct, Enum
	{
		/// <summary>
		/// Add the given <paramref name="id"/> <paramref name="material"/> to the service
		/// </summary>
		void Add(T id, Material material);
		
		/// <summary>
		/// Removes the given <paramref name="id"/>'s <see cref="Material"/> from the service
		/// </summary>
		void Remove(T id);
		
		/// <summary>
		/// Clears the container of materials currently held by this service
		/// </summary>
		Dictionary<T, Material> Clear();
	}
	
	/// <inheritdoc />
	public class MaterialVfxService<T> : IMaterialVfxInternalService<T> where T : struct, Enum
	{
		private readonly IDictionary<T, Material> _materials = new Dictionary<T, Material>();
		
		/// <inheritdoc />
		public Material Get(T id)
		{
			return new Material(_materials[id]);
		}
		
		/// <inheritdoc />
		public void Add(T id, Material material)
		{
			_materials.Add(id, material);
		}

		/// <inheritdoc />
		public void Remove(T id)
		{
			_materials.Remove(id);
		}

		/// <inheritdoc />
		public Dictionary<T, Material> Clear()
		{
			var dic = new Dictionary<T, Material>(_materials);
			
			_materials.Clear();

			return dic;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_materials.Clear();
		}
	}
}