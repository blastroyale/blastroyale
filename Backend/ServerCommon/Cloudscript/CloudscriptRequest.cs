using System;
using System.ComponentModel.DataAnnotations;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Modules;
using PlayFab;

namespace ContainerApp.Cloudscript
{
    /// <summary>
	/// Objects that represent cloudscript response formats
	/// </summary>
	[Serializable]
	public class CloudscriptRequest
	{
		[Required(ErrorMessage = "Caller entity profile is required")]
		public PlayfabEntityProfile? CallerEntityProfile { get; set; }
		
		public LogicRequest? FunctionArgument { get; set; }
		
		public string PlayfabId => CallerEntityProfile?.Lineage?.MasterPlayerAccountId;
        
        public PlayFabAuthenticationContext GetAuthContext()
        {
           
            var authContext = ModelSerializer.DeserializeFromData<PlayFabAuthenticationContext>(FunctionArgument.Data);
            if (authContext == null)
            {
                throw new Exception("AuthenticationContext key not present in function argument data");
            }
            return authContext;
        }
	}

	[Serializable]
	public class PlayfabEntityProfile
	{
		[Required(ErrorMessage = "Entity is Required")]
		public PlayfabEntity? Entity { get; set; }
		
		[Required(ErrorMessage = "Lineage is Required")]
		public PlayfabLineage? Lineage { get; set; }
	}

	[Serializable]
	public class PlayfabEntity 
	{
		[Required(ErrorMessage = "Entity ID is required")]
		public string? Id { get; set; }
	}

    [Serializable]
	public class PlayfabLineage
	{
		[Required(ErrorMessage = "MasterPlayerAccountId is required")]
		public string? MasterPlayerAccountId { get; set; }
		
		[Required(ErrorMessage = "TitlePlayerAccountId is required")]
		public string? TitlePlayerAccountId { get; set; }
	}
}

