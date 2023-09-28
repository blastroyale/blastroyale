using System.Linq;
using System.Threading.Tasks;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Utils;
using FirstLightServerSDK.Modules.RemoteCollection;
using FirstLightServerSDK.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service responsible for enrhicing collectiondata model with remote data
	/// from third party services.
	/// 
	/// Flow:
	/// - Client receives data models
	/// - Client checks if data model is IEnrichableData
	/// - Client calls registered IRemoteCollectionAdapter to obtain extra remote data
	/// - Client merges data to the IEnrichableData data model  
	/// </summary>
	public class CollectionEnrichmentService : ICollectionEnrichmentService
	{
		private IGameBackendService _backend;
		private IGameDataProvider _data;
		private IRemoteCollectionAdapter _adapter;


		public CollectionEnrichmentService(IGameBackendService backend, IGameDataProvider data)
		{
			_adapter = new PlayfabRemoteCollectionAdapter(backend);
			_backend = backend;
			_data = data;
		}

		public IRemoteCollectionAdapter GetAdapter() => _adapter;

		public void Enrich<T>(T clientData) where T : IEnrichableData
		{
			if (!FeatureFlags.REMOTE_COLLECTIONS)
			{
				return;
			}

			var services = MainInstaller.Resolve<IGameServices>();
			foreach (var collectionType in clientData.GetEnrichedTypes())
			{
				_adapter.FetchOwned(collectionType, list =>
				{
					foreach (var owned in list) clientData.Enrich(owned);
					services.MessageBrokerService.Publish(new CollectionEnrichedMessage()
					{
						CollectionType = collectionType,
						DataType = clientData.GetType()
					});
				});
			}
		}
	}
}