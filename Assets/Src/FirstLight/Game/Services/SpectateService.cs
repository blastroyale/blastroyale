using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public struct SpectatedPlayer
	{
		public EntityRef Entity;
		public PlayerRef Player;
		public Transform Transform;

		public SpectatedPlayer(EntityRef entity, PlayerRef player, Transform transform)
		{
			Entity = entity;
			Player = player;
			Transform = transform;
		}
	}
	
	/// <summary>
	/// Tracks the currently spectated player / entity, and allows operations on it.
	///
	/// This should be used wherever we are displaying any information about to the "current player".
	/// </summary>
	public interface ISpectateService
	{
		/// <summary>
		/// The currently spectated player, providing it's <see cref="EntityRef"/>, <see cref="PlayerRef"/> and
		/// <see cref="Transform"/>.
		/// </summary>
		IObservableFieldReader<SpectatedPlayer> SpectatedPlayer { get; }

		/// <summary>
		/// Starts spectating the next player.
		/// </summary>
		public void SwipeLeft();

		/// <summary>
		/// Starts spectating the previous player.
		/// </summary>
		public void SwipeRight();
	}

	public class SpectateService : ISpectateService, MatchServices.IMatchService
	{
		private readonly IGameServices _gameServices;
		private readonly IMatchServices _matchServices;
		private readonly FP _playerVisionRange;
		private readonly IObservableField<SpectatedPlayer> _spectatedPlayer = new ObservableField<SpectatedPlayer>();
		
		public IObservableFieldReader<SpectatedPlayer> SpectatedPlayer => _spectatedPlayer;

		public SpectateService(IGameServices gameServices, IMatchServices matchServices)
		{
			_gameServices = gameServices;
			_matchServices = matchServices;
			_playerVisionRange = gameServices.ConfigsProvider.GetConfig<QuantumGameConfig>().PlayerVisionRange;

			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnQuantumUpdateView);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnEventOnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned); // For Deathmatch
		}

		public void Dispose()
		{
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			if (_gameServices.NetworkService.IsSpectorPlayer)
			{
				SwipeRight(game);

				return;
			}
			
			var localPlayer = game.GetLocalPlayerData(false, out var f);

			if (isReconnect && !localPlayer.Entity.IsAlive(f))
			{
				SwipeRight(game);
			}
			else
			{
				SetSpectatedEntity(localPlayer.Entity, localPlayer.Player);
			}
		}

		private void OnQuantumUpdateView(CallbackUpdateView callback)
		{
			// This stupidity along with all the TryGetNextPlayer nonsense is needed because apparently Quantum lags
			// behind when we're in Spectate mode, meaning that we aren't able to fetch the initial spectated player
			// on the first frame the same way we can in normal mode. SMH.
			TrySetSpectateModePlayer(callback.Game);

			if (_spectatedPlayer.Value.Transform != null)
			{
				callback.Game.SetPredictionArea(_spectatedPlayer.Value.Transform.position.ToFPVector3(),
				                                _playerVisionRange);
			}
		}

		public void OnMatchEnded()
		{
			_spectatedPlayer.StopObservingAll();

			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		public void SwipeLeft()
		{
			SwipeLeft(QuantumRunner.Default.Game);
		}

		public void SwipeRight()
		{
			SwipeRight(QuantumRunner.Default.Game);
		}

		private void SwipeLeft(QuantumGame game)
		{
			TryGetPreviousPlayer(game, out var player);
			SetSpectatedEntity(player.Key, player.Value);
		}

		private void SwipeRight(QuantumGame game)
		{
			TryGetNextPlayer(game, out var player);
			SetSpectatedEntity(player.Key, player.Value);
		}

		private bool TryGetNextPlayer(QuantumGame game, out Pair<EntityRef, PlayerRef> player)
		{
			var frame = game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			if (players.Count > 0)
			{
				player = players[currentIndex + 1 < players.Count ? currentIndex + 1 : 0];
				return true;
			}

			player = default;
			return false;
		}

		private bool TryGetPreviousPlayer(QuantumGame game, out Pair<EntityRef, PlayerRef> player)
		{
			var frame = game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			if (players.Count > 0)
			{
				player = players[currentIndex - 1 >= 0 ? currentIndex - 1 : players.Count - 1];
				return true;
			}

			player = default;
			return false;
		}

		private void TrySetSpectateModePlayer(QuantumGame game)
		{
			// Spectator mode - set new player to follow, only once
			if (_gameServices.NetworkService.QuantumClient.LocalPlayer.IsSpectator() && !_spectatedPlayer.Value.Entity.IsValid &&
			    TryGetNextPlayer(game, out var player))
			{
				SetSpectatedEntity(player.Key, player.Value, true);
			}
		}

		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			if (callback.Entity != _spectatedPlayer.Value.Entity)
			{
				return;
			}

			if(!callback.Game.Frames.Verified.TryGet<PlayerCharacter>(callback.EntityKiller, out var killerPlayer) || 
			        !SetSpectatedEntity(callback.EntityKiller, killerPlayer.Player))
			{
				SwipeRight(callback.Game);
			}
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			SetSpectatedEntity(callback.Entity, callback.Player);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			SetSpectatedEntity(callback.Entity, callback.Player);
		}

		private bool SetSpectatedEntity(EntityRef entity, PlayerRef player, bool safe = false)
		{
			if (_spectatedPlayer.Value.Entity == entity || !entity.IsValid) return false;

			if (_matchServices.EntityViewUpdaterService.TryGetView(entity, out var view))
			{
				_spectatedPlayer.Value = new SpectatedPlayer(entity, player, view.transform);
				
				return true;
			}
			
			if (!safe)
			{
				throw new Exception($"Could not fetch EntityView for {entity}");
			}

			return false;
		}

		private List<Pair<EntityRef, PlayerRef>> GetPlayerList(Frame f, out int currentIndex)
		{
			var players = new List<Pair<EntityRef, PlayerRef>>();
			var container = f.GetSingleton<GameContainer>();
			var playersData = container.PlayersData;
			currentIndex = -1;

			for (int i = 0; i < playersData.Length; i++)
			{
				var data = playersData[i];
				if (data.IsValid && data.Entity.IsAlive(f))
				{
					players.Add(new Pair<EntityRef, PlayerRef>(data.Entity, data.Player));

					if (_spectatedPlayer.Value.Entity == data.Entity)
					{
						currentIndex = players.Count - 1;
					}
				}
			}

			return players;
		}
	}
}