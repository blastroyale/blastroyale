using System;
using System.Collections.Generic;
using FirstLightServerSDK.Modules.RemoteCollection;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// This interface provides the access to the player's save persistent data 
	/// </summary>
	public interface IDataProvider
	{
		/// <summary>
		/// Generic wrapper of <see cref="TryGetData"/>
		/// </summary>
		bool TryGetData<T>(out T dat) where T : class;

		/// <summary>
		/// Requests the player's data of <paramref name="type"/> type
		/// </summary>
		bool TryGetData(Type type, out object dat);

		/// <summary>
		/// Generic wrapper of <see cref="GetData"/>
		/// </summary>
		T GetData<T>() where T : class;

		/// <summary>
		/// Requests the player's data of <paramref name="type"/>
		/// </summary>
		object GetData(Type type);
		
		/// <summary>
		///  Return all the keys of the present data
		/// </summary>
		IEnumerable<Type> GetKeys();

		/// <summary>
		/// Gets the instance of the data enrichment service.
		/// This service is the one who knows how to enrich remote collection data
		/// when models stored in the data provider are flagged as remotely enricheable.
		/// Only used for "real time" data enrichment.
		/// </summary>
		ICollectionEnrichmentService GetEnrichmentService();
	}

}