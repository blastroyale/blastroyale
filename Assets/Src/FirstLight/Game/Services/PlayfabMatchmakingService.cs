using System;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Hanldles matchmaking flow, that is, the flow for players to find another players to join a match.
	/// The flow is:
	/// - Player obtains a "Matchmaking Ticket"
	/// - Players either subscribes to notifications or polls on this ticket to see when its ready
	/// - When its ready player will have a identifier to know which game he needs to join.
	/// </summary>
	public interface IMatchmakingService
	{
		// TODO: Move me 
		Vector2 NormalizedMapSelectedPosition { get; set; }
		
		/// <summary>
		/// Leaves all matchmaking tickets that are being waited
		/// </summary>
		public void LeaveMatchmaking();

		/// <summary>
		/// Obtains a given matchmaking ticket the ticket information
		/// </summary>
		public void GetTicket(string ticket, Action<GetMatchmakingTicketResult> callback);

		/// <summary>
		/// Get a list of all my current active tickets
		/// </summary>
		public void GetMyTickets(Action<ListMatchmakingTicketsForPlayerResult> callback);

		/// <summary>
		/// Joins matchmaking queue
		/// </summary>
		public void JoinMatchmaking(Action<CreateMatchmakingTicketResult> callback);
	}
	
	/// <inheritdoc cref="IMatchmakingService"/>
	public class PlayfabMatchmakingService : IMatchmakingService
	{
		private static string QUEUE_NAME = "Matchmaking_Queue"; // TODO: Drive from outside for multiple q 
		private IPlayfabService _playfab;
		
		public PlayfabMatchmakingService(IPlayfabService playfab)
		{
			_playfab = playfab;
		}
		
		/// <summary>
		/// Returns the player's selected point on the map in a normalized state
		/// </summary>
		public Vector2 NormalizedMapSelectedPosition { get; set; }

		public void LeaveMatchmaking()
		{
			PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest(),null, _playfab.HandleError);
		}

		public void GetTicket(string ticket, Action<GetMatchmakingTicketResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest()
			{
				QueueName = QUEUE_NAME,
				TicketId = ticket
			}, callback, _playfab.HandleError);
		}

		public void GetMyTickets(Action<ListMatchmakingTicketsForPlayerResult> callback)
		{
			PlayFabMultiplayerAPI.ListMatchmakingTicketsForPlayer(new ListMatchmakingTicketsForPlayerRequest(), callback, 
				_playfab.HandleError);
		}

		public void JoinMatchmaking(Action<CreateMatchmakingTicketResult> callback)
		{
			PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest(), callback,
				_playfab.HandleError);
		}
	}
}