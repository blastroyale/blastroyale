using System;
using System.Net.Http;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.Json;
using PlayFab.Plugins.CloudScript;

namespace Backend.Util
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

		/// <summary>
		/// Process and converts the given <see cref="HttpRequestMessage"/> <paramref name="req"/> into a readable
		/// <see cref="PlayStreamFunctionContext"/>
		/// </summary>
		/// <remarks>
		/// Use this only for PlayStream rule trigger events
		/// </remarks>
		public static async Task<PlayStreamFunctionContext> ProcessPlayStreamContext(HttpRequestMessage req)
		{
			var context = PlayFabSimpleJson.DeserializeObject<PlayerPlayStreamFunctionExecutionContext>(await req.Content.ReadAsStringAsync());

			return new PlayStreamFunctionContext
			{
				PlayStreamContext = context,
				PlayFabId = context.PlayStreamEventEnvelope.EntityId,
				AuthenticationContext = new PlayFabAuthenticationContext { EntityToken = context.TitleAuthenticationContext.EntityToken },
				ApiSettings = new PlayFabApiSettings 
				{ 
					TitleId = context.TitleAuthenticationContext.Id,
					DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY",EnvironmentVariableTarget.Process),
				}
			};
		}
	}
}