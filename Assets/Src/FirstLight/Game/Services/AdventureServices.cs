using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Provides access to all adventure's common helper services
	/// This services are stateless interfaces that establishes a set of available operations with deterministic response
	/// without manipulating any game’s data
	/// </summary>
	/// <remarks>
	/// Follows the "Service Locator Pattern" <see cref="https://www.geeksforgeeks.org/service-locator-pattern/"/>
	/// </remarks>
	public interface IAdventureServices : IDisposable
	{
	}
	
	/// <inheritdoc />
	public class AdventureServices : IAdventureServices
	{
		private readonly IList<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
		private readonly IAssetResolverService _assetResolverService;

		public AdventureServices(IAssetResolverService assetResolverService)
		{
			_assetResolverService = assetResolverService;
		}
		
		/// <inheritdoc />
		public void Dispose()
		{
			for (var i = 0; i < _handles.Count; i++)
			{
				if (_handles[i].IsValid())
				{
					Addressables.Release(_handles[i]);
				}
			}
			
			_handles.Clear();
		}
	}
}