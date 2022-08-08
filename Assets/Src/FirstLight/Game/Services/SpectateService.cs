using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
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

	public class SpectateService : ISpectateService, MatchServices.IMatchService
	{
		public IObservableFieldReader<SpectatedPlayer> SpectatedPlayer => _spectatedPlayer;

		private readonly IEntityViewUpdaterService _entityViewUpdaterService;
		private readonly IGameNetworkService _networkService;

		private readonly FP _playerVisionRange;

		private readonly IObservableField<SpectatedPlayer> _spectatedPlayer = new ObservableField<SpectatedPlayer>();

		public SpectateService(IEntityViewUpdaterService entityViewUpdaterService, IGameNetworkService networkService,
		                       IConfigsProvider configsProvider)
		{
			_entityViewUpdaterService = entityViewUpdaterService;
			_networkService = networkService;

			_playerVisionRange = configsProvider.GetConfig<QuantumGameConfig>().PlayerVisionRange;
		}

		public void OnMatchStarted(bool isReconnect)
		{
			if (_networkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				if (isReconnect)
				{
					SwipeRight();
				}
			}
			else
			{
				var game = QuantumRunner.Default.Game;
				var f = QuantumRunner.Default.Game.Frames.Verified;
				var gameContainer = f.GetSingleton<GameContainer>();
				var playersData = gameContainer.PlayersData;

				var localPlayer = playersData[game.GetLocalPlayers()[0]];

				if (isReconnect && !localPlayer.Entity.IsAlive(f))
				{
					SwipeRight();
				}
				else
				{
					SetSpectatedEntity(localPlayer.Entity, localPlayer.Player);
				}
			}

			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnQuantumUpdateView);

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnHealthIsZero>(this, OnEventOnHealthIsZero);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned); // For Deathmatch
		}

		private void OnQuantumUpdateView(CallbackUpdateView callback)
		{
			// This stupidity along with all the TryGetNextPlayer nonsense is needed because apparently Quantum lags
			// behind when we're in Spectate mode, meaning that we aren't able to fetch the initial spectated player
			// on the first frame the same way we can in normal mode. SMH.
			TrySetSpectateModePlayer();

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
			TryGetPreviousPlayer(out var player);
			SetSpectatedEntity(player.Key, player.Value);
		}

		public void SwipeRight()
		{
			TryGetNextPlayer(out var player);
			SetSpectatedEntity(player.Key, player.Value);
		}

		private bool TryGetNextPlayer(out Pair<EntityRef, PlayerRef> player)
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			if (players.Count > 0)
			{
				player = players[currentIndex + 1 < players.Count ? currentIndex + 1 : 0];
				return true;
			}

			player = default;
			return false;
		}

		private bool TryGetPreviousPlayer(out Pair<EntityRef, PlayerRef> player)
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			if (players.Count > 0)
			{
				player = players[currentIndex - 1 >= 0 ? currentIndex - 1 : players.Count - 1];
				return true;
			}

			player = default;
			return false;
		}

		private void TrySetSpectateModePlayer()
		{
			// Spectator mode - set new player to follow, only once
			if (_networkService.QuantumClient.LocalPlayer.IsSpectator() && !_spectatedPlayer.Value.Entity.IsValid &&
			    TryGetNextPlayer(out var player))
			{
				SetSpectatedEntity(player.Key, player.Value, true);
			}
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityDead != _spectatedPlayer.Value.Entity)
			{
				return;
			}

			if (callback.EntityDead == callback.EntityKiller)
			{
				SetSpectatedEntity(callback.EntityLeader, callback.PlayerLeader);
			}
			else if (callback.Game.Frames.Verified.Has<DeadPlayerCharacter>(callback.EntityKiller))
			{
				SwipeRight();
			}
			else
			{
				SetSpectatedEntity(callback.EntityKiller, callback.PlayerKiller);
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

		private void OnEventOnHealthIsZero(EventOnHealthIsZero callback)
		{
			if (callback.Entity == _spectatedPlayer.Value.Entity && callback.SpellID == Spell.ShrinkingCircleId)
			{
				SwipeRight();
			}
		}

		private void SetSpectatedEntity(EntityRef entity, PlayerRef player, bool safe = false)
		{
			if (_spectatedPlayer.Value.Entity == entity) return;

			if (_entityViewUpdaterService.TryGetView(entity, out var view))
			{
				_spectatedPlayer.Value = new SpectatedPlayer(entity, player, view.transform);
			}
			else if (!safe)
			{
				throw new Exception($"Could not fetch EntityView for {entity}");
			}
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