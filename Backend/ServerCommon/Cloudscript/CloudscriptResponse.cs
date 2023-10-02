using System;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules;
using PlayFab;

namespace ServerCommon.Cloudscript
{
	/// <summary>
	/// Object that represents Cloudscript response formats.
	/// </summary>
	[Serializable]
	public class CloudscriptResponse
	{
		public BackendLogicResult Result;
		
		public CloudscriptResponse(PlayFabResult<BackendLogicResult> playfabResult)
		{
			Result = playfabResult?.Result;
		}

        /// <summary>
        /// Playfab does not allow us to return error messages in case we dont return 200 (OK) status
        /// This function wraps an exception in a valid 200 response so client can understand the error.
        /// </summary>
        public static CloudscriptResponse FromError(Exception e)
        {
            return FromData(new()
            {
                { "LogicException", e.Message }
            });
        }
        
        /// <summary>
        /// Builds a cloudscript response object format from a given data dictionary.
        /// </summary>
        public static CloudscriptResponse FromData(Dictionary<string, string> data)
        {
            return new CloudscriptResponse(new PlayFabResult<BackendLogicResult>()
            {
                Result = new BackendErrorResult()
                {
                    Data = data
                }
            });
        }
		
		/// <summary>
		/// Builds a cloudscript response object format from a given object embedded in response.
		/// </summary>
		public static CloudscriptResponse FromObject<T>(T obj)
		{
			var data = new Dictionary<string, string>();
			ModelSerializer.SerializeToData(data, obj);
			return FromData(data);
		}
	}
}