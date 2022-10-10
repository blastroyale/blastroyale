using System;
using FirstLight.Game.Logic;
using PlayFab;

namespace ContainerApp.Cloudscript
{
	/// <summary>
	/// Object that represents Cloudscript response formats.
	/// </summary>
	[Serializable]
	public class CloudscriptResponse
	{
		public BackendLogicResult Result { get; set; }
		
		public CloudscriptResponse(PlayFabResult<BackendLogicResult> playfabResult)
		{
			Result = playfabResult.Result;
		}
	}
}