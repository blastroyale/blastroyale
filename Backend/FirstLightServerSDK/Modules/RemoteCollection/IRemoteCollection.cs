using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Server.SDK.Models;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// Flags the data model as enrichable by the given set of enrichment types.
	/// </summary>
	public interface IEnrichableData
	{
		public void Enrich(RemoteCollectionItem data);
		public bool NeedsEnrichment();
		public Type[] GetEnrichedTypes();
	}

	/// <summary>
	/// Needs to be implemented to fetch remote collections.
	/// Might differ implementations on client and server.
	/// </summary>
	public interface IRemoteCollectionAdapter
	{
		void FetchOwned(Type type, Action<IEnumerable<RemoteCollectionItem>> cb);
		void FetchAll(Type type, Action<IEnumerable<RemoteCollectionItem>> cb);
	}

	/// <summary>
	/// Data model that can be enriched by remote data.
	/// This is for client data models that requires externals sources to be enriched
	/// </summary>
	public abstract class CollectionItemEnrichmentData : IEnrichableData
	{
		public abstract Type[] GetEnrichedTypes();
		private bool _enriched;
		public void Enrich(RemoteCollectionItem data)
		{
			if (GetEnrichedTypes().Any(i => i == data.GetType()))
			{
				EnrichFromType(data.GetType(), data);
			}
			_enriched = true;
		} 
		protected abstract void EnrichFromType(Type type, RemoteCollectionItem remoteData);
		
		public bool NeedsEnrichment() => !_enriched;
	}

}