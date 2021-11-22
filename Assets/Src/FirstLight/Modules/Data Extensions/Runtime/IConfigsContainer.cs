using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	/// <summary>
	/// Generic contract for a config to be serialized in containers
	/// </summary>
	public interface IConfig
	{
		int ConfigId { get; }
	}
	
	/// <summary>
	/// Generic container of the configs imported with a ConfigsImporter script
	/// The given <typeparamref name="T"/> type is the same of the config struct defined to be serialized in the scriptable object
	/// </summary>
	public interface IConfigsContainer<T> where T : struct
	{
		List<T> Configs { get; set; }
	}
	
	/// <summary>
	/// Generic container of the unique single config imported with a ConfigsImporter script
	/// The given <typeparamref name="T"/> type is the same of the config struct defined to be serialized in the scriptable object
	/// </summary>
	public interface ISingleConfigContainer<T> where T : struct
	{
		T Config { get; set; }
	}
	
	/// <inheritdoc />
	/// <remarks>
	/// Use this configs container to hold the configs data in pairs mapped by it's given <typeparamref name="TKey"/> id
	/// </remarks>
	public interface IPairConfigsContainer<TKey, TValue> : IConfigsContainer<Pair<TKey, TValue>> 
		where TKey : struct
	{
	}
}