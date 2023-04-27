using FirstLightServerSDK.Services;

namespace FirstLightServerSDK.Modules.RemoteCollection
{
	/// <summary>
	/// Enriches a given data model using remote collection types.
	/// </summary>
	public interface ICollectionEnrichmentService
	{
		IRemoteCollectionAdapter GetAdapter();
		void Enrich<T>(T clientData) where T : IEnrichableData;
	}
}