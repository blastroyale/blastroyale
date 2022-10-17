using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Internal;

namespace ServerCommon
{
	/// <summary>
	/// Interface to handle any errors that happens during logic execution.
	/// Should always wrap the error in a LogicException.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IErrorService<T>
	{
		/// <summary>
		/// Handles potential errors.
		/// </summary>
		public void CheckErrors<T>(PlayFabResult<T> result) where T : PlayFabResultCommon;
	}

	/// <inheritdoc />
	public class PlayfabErrorService : IErrorService<PlayFabError>
	{
		private readonly ILogger _log;
	
		public PlayfabErrorService(ILogger log)
		{
			_log = log;
		}

		public void CheckErrors<T>(PlayFabResult<T> result) where T : PlayFabResultCommon
		{
			if (result.Error != null)
			{
				throw new LogicException($"Playfab Error {result.Error.ErrorMessage}: {JsonConvert.SerializeObject(result.Error.ErrorDetails)}");
			}
		}
	}
}

