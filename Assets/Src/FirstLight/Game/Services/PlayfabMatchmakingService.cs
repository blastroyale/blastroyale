using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.MultiplayerModels;
using Quantum;
using System.Threading.Tasks;
using FirstLight.FLogger;
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
		public void JoinMatchmaking(MatchRoomSetup setup);

		/// <summary>
		/// Invokes that a game was found
		/// </summary>
		public void InvokeMatchFound(GameMatched match);

		public delegate void OnGameMatchedEventHandler(GameMatched match);

		public event OnGameMatchedEventHandler OnGameMatched;
		
	}

	public class MatchRoomSetup
	{
		public QuantumGameModeConfig GameMode;
		public QuantumMapConfig Map;
		public IReadOnlyList<string> Mutators;
		public MatchType MatchType;
		
		/// <summary>
		/// Identifier for room names to ensure players can join the same room
		/// </summary>
		[CanBeNull] public string RoomIdentifier;
	}
	
	public class GameMatched
	{
		public string MatchIdentifier;
		public MatchRoomSetup RoomSetup;
	}
	
	/// <inheritdoc cref="IMatchmakingService"/>
	public class PlayfabMatchmakingService : IMatchmakingService
	{
		private static string QUEUE_NAME = "flgranked"; // TODO: Drive from outside for multiple q 
		private IGameBackendService _gameBackend;
		private ICoroutineService _coroutines;
		private MatchmakingPooling _pooling;
		
		public PlayfabMatchmakingService(IGameBackendService gameBackend, ICoroutineService coroutines)
		{
			_gameBackend = gameBackend;
			_coroutines = coroutines;
		}
		
		/// <summary>
		/// Returns the player's selected point on the map in a normalized state
		/// </summary>
		public Vector2 NormalizedMapSelectedPosition { get; set; }
		
		public void LeaveMatchmaking()
		{
			PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(new CancelAllMatchmakingTicketsForPlayerRequest()
			{
				QueueName = QUEUE_NAME
			},null, _gameBackend.HandleError);
			FLog.Verbose("Left Matchmaking");
		}

		public void GetTicket(string ticket, Action<GetMatchmakingTicketResult> callback)
		{
			PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest()
			{
				QueueName = QUEUE_NAME,
				TicketId = ticket
			}, callback, _gameBackend.HandleError);
		}

		public void GetMyTickets(Action<ListMatchmakingTicketsForPlayerResult> callback)
		{
			PlayFabMultiplayerAPI.ListMatchmakingTicketsForPlayer(new ListMatchmakingTicketsForPlayerRequest()
				{
					QueueName = QUEUE_NAME
				}, callback, 
				_gameBackend.HandleError);
		}

		public void JoinMatchmaking(MatchRoomSetup setup)
		{
			PlayFabMultiplayerAPI.CreateMatchmakingTicket(new CreateMatchmakingTicketRequest()
			{
				MembersToMatchWith = null, // HERE IS WHERE WE ADD THE SQUAD !!!
				QueueName = QUEUE_NAME,
				GiveUpAfterSeconds = 1000,
				Creator = new MatchmakingPlayer()
				{
					Entity = new EntityKey()
					{
						Id = PlayFabSettings.staticPlayer.EntityId,
						Type = PlayFabSettings.staticPlayer.EntityType
					}
 				}
			}, r =>
			{
				if (_pooling != null)
				{
					_pooling.Stop();
				}
				_pooling = new MatchmakingPooling(r.TicketId, setup, this, _coroutines);
				_pooling.Start();
			},
			_gameBackend.HandleError);
			
		}

		public void InvokeMatchFound(GameMatched match)
		{
			match.RoomSetup.RoomIdentifier = match.MatchIdentifier;
			OnGameMatched?.Invoke(match);
		}

		public event IMatchmakingService.OnGameMatchedEventHandler OnGameMatched;
	}

	/// <summary>
	/// Basic matchmaking pooling to check whenever our match is ready.
	/// SHould be replaced with websockets notification soon
	/// </summary>
	public class MatchmakingPooling
	{
		private string _ticket;
		private MatchRoomSetup _setup;
		private IMatchmakingService _service;
		private ICoroutineService _routines;
		private Coroutine _task;
		private bool _pooling = false;

		public MatchmakingPooling(string ticket, MatchRoomSetup setup, IMatchmakingService service, ICoroutineService coroutines)
		{
			_ticket = ticket;
			_service = service;
			_routines = coroutines;
			_setup = setup;
		}

		public void Start()
		{
			_task = _routines.StartCoroutine(Runnable());
		}

		public void Stop()
		{
			_routines.StopCoroutine(_task);
		}

		private IEnumerator Runnable()
		{
			var delay = new WaitForSeconds(7);
			_pooling = true;
			while (_pooling)
			{
				_service.GetTicket(_ticket, ticket =>
				{
					Debug.Log("Ticket Pool: "+JsonConvert.SerializeObject(ticket));
					// TODO: Check when ticket expired and expose event
					if (ticket.Status == "Matched")
					{
						// TODO: Invoke this when websocket is received
						_service.InvokeMatchFound(new GameMatched()
						{
							MatchIdentifier = ticket.MatchId,
							RoomSetup = _setup
						});
						_pooling = false;
					}
				});
				yield return delay;
			}
		}
	}
}

