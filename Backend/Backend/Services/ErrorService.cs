using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;

namespace Backend.Game.Services
{
	/// <summary>
	/// Interface to handle any errors that happens during logic execution.
	/// Should always wrap the error in a LogicException.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IErrorService<T>
	{
		/// <summary>
		/// Handles error, logs it. Convert any type of error to a Logic Exception.
		/// </summary>
		public LogicException HandleError(T error);
	}

	/// <inheritdoc />
	public class PlayfabErrorService : IErrorService<PlayFabError>
	{
		private readonly ILogger _log;
	
		public PlayfabErrorService(ILogger log)
		{
			_log = log;
		}
	
		/// <inheritdoc />
		public LogicException HandleError(PlayFabError error)
		{
			return new LogicException($"Playfab Error {error.ErrorMessage}: {JsonConvert.SerializeObject(error.ErrorDetails)}");
		}
	}
}

