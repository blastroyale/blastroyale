using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Models.Collection;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Services;
using MessagePack;
using PlayFab.CloudScriptModels;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Adapter to fetch remote collection data trough playfab cloudscript
	/// </summary>
	public class PlayfabRemoteCollectionAdapter : IRemoteCollectionAdapter
	{
		private IGameBackendService _service;

		public PlayfabRemoteCollectionAdapter(IGameBackendService service)
		{
			_service = service;
		}

		private IEnumerable<RemoteCollectionItem> DeserializeResponse(Type t, ExecuteFunctionResult res)
		{
			var base64 = res.FunctionResult as string;
			return CollectionSerializer.Deserialize(base64).Owned;
		}

		public void FetchOwned(Type type, Action<IEnumerable<RemoteCollectionItem>> cb)
		{
			_service.CallFunction("GetOwnedCollection", r => cb(DeserializeResponse(type, r))
				, null, new CollectionFetchRequest()
				{
					CollectionName = type.Name,
					Data = new()
				});
		}

		public void FetchAll(Type type, Action<IEnumerable<RemoteCollectionItem>> cb)
		{
			throw new NotImplementedException("COMING SOON [TM]");
		}
		
	}
}