using System;
using System.Net.Http;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.Json;
using PlayFab.Plugins.CloudScript;

namespace Backend.Context
{
	/// <summary>
	/// This object is a data container for PlayStream context triggered events
	/// </summary>
	public class PlayStreamFunctionContext
	{
		public PlayerPlayStreamFunctionExecutionContext PlayStreamContext;
		public PlayFabAuthenticationContext AuthenticationContext;
		public PlayFabApiSettings ApiSettings;
		public string PlayFabId;
	}

	/// <summary>
	/// This object helps process http requests into their proper context
	/// </summary>
	public static class ContextProcessor
	{
		/// <summary>
		/// Process and converts the given <see cref="HttpRequestMessage"/> <paramref name="req"/> into a readable
		/// <see cref="FunctionContext{TFunctionArgument}"/> of the given <typeparamref name="T"/>
		/// </summary>
		public static async Task<FunctionContext<T>> ProcessContext<T>(HttpRequestMessage req)
		{
			var context = await FunctionContext<T>.Create(req);
			
			context.AuthenticationContext.EntityId = context.CallerEntityProfile.Entity.Id;
			context.AuthenticationContext.EntityType = context.CallerEntityProfile.Entity.Type;
			context.AuthenticationContext.PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId;

			return context;
		}
	}
}

