using System;
using System.Collections.Generic;
using FirstLight.Game.Messages;
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
		public int Team;
		public Transform Transform;

		public SpectatedPlayer(EntityRef entity, PlayerRef player, int team, Transform transform)
		{
			Entity = entity;
			Player = player;
			Team = team;
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

			_gameServices.MessageBrokerService.Subscribe<MatchSimulationEndedMessage>(OnMatchSimulationEnded);

			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnQuantumUpdateView);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnEventOnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned); // For Deathmatch
		}

		public void Dispose()
		{
			_gameServices?.MessageBrokerService?.UnsubscribeAll(this);
			_spectatedPlayer.StopObservingAll();
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			if (_gameServices.NetworkService.LocalPlayer.IsSpectator())
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
				SetSpectatedEntity(game.Frames.Verified, localPlayer.Entity, localPlayer.Player);
			}
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			SetSpectatedEntity(null, EntityRef.None, PlayerRef.None, true);
		}

		public void OnMatchSimulationEnded(MatchSimulationEndedMessage message)
		{
			SetSpectatedEntity(null, EntityRef.None, PlayerRef.None, true);
		}

		public void SwipeLeft()
		{
			SwipeLeft(QuantumRunner.Default.Game);
		}

		public void SwipeRight()
		{
			SwipeRight(QuantumRunner.Default.Game);
		}

		private unsafe void OnQuantumUpdateView(CallbackUpdateView callback)
		{
			var game = callback.Game;

			// This stupidity along with all the TryGetNextPlayer nonsense is needed because apparently Quantum lags
			// behind when we're in Spectate mode, meaning that we aren't able to fetch the initial spectated player
			// on the first frame the same way we can in normal mode. SMH.
			if (!_spectatedPlayer.Value.Entity.IsValid &&
				_gameServices.NetworkService.LocalPlayer.IsSpectator() &&
				TryGetNextPlayer(game, out var player))
			{
				SetSpectatedEntity(callback.Game.Frames.Verified, player.Entity, player.Player, true);
			}

			if (game.Frames.Predicted.Unsafe.TryGetPointer<Transform3D>(_spectatedPlayer.Value.Entity,
					out var transform3D))
			{
				game.SetPredictionArea(transform3D->Position, _playerVisionRange);
			}
		}

		private void SwipeLeft(QuantumGame game)
		{
			TryGetPreviousPlayer(game, out var player);
			SetSpectatedEntity(game.Frames.Verified, player.Entity, player.Player);
		}

		private void SwipeRight(QuantumGame game)
		{
			TryGetNextPlayer(game, out var player);
			SetSpectatedEntity(game.Frames.Verified, player.Entity, player.Player);
		}

		private bool TryGetNextPlayer(QuantumGame game, out Quantum.PlayerMatchData player)
		{
			var players = GetPlayerList(game, out var currentIndex);

			if (players.Count > 0)
			{
				player = players[currentIndex + 1 < players.Count ? currentIndex + 1 : 0];
				return true;
			}

			player = default;
			return false;
		}

		private bool TryGetPreviousPlayer(QuantumGame game, out Quantum.PlayerMatchData player)
		{
			var players = GetPlayerList(game, out var currentIndex);

			if (players.Count > 0)
			{
				player = players[currentIndex - 1 >= 0 ? currentIndex - 1 : players.Count - 1];
				return true;
			}

			player = default;
			return false;
		}

		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			if (callback.Entity != _spectatedPlayer.Value.Entity)
			{
				return;
			}

			if (!callback.Game.Frames.Verified.TryGet<PlayerCharacter>(callback.EntityKiller, out var killerPlayer) ||
				GetLivingTeamMembers(callback.Game).Count > 0 ||
				!SetSpectatedEntity(callback.Game.Frames.Verified, callback.EntityKiller, killerPlayer.Player))
			{
				SwipeRight(callback.Game);
			}
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			SetSpectatedEntity(callback.Game.Frames.Verified, callback.Entity, callback.Player);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			SetSpectatedEntity(callback.Game.Frames.Verified, callback.Entity, callback.Player);
		}

		private bool SetSpectatedEntity(Frame f, EntityRef entity, PlayerRef player, bool safe = false)
		{
			if (f != null && _matchServices.EntityViewUpdaterService.TryGetView(entity, out var view))
			{
				var team = f.TryGet<Targetable>(entity, out var t) ? t.Team : -1;
				_spectatedPlayer.Value = new SpectatedPlayer(entity, player, team, view.transform);

				return true;
			}

			_spectatedPlayer.Value = new SpectatedPlayer();

			if (entity.IsValid && !safe)
			{
				throw new Exception($"Could not fetch EntityView for {entity}");
			}

			return false;
		}

		private List<Quantum.PlayerMatchData> GetLivingTeamMembers(QuantumGame game)
		{
			var localPlayer = game.GetLocalPlayerData(true, out var f);
			var localTeamId = localPlayer.TeamId;

			var container = f.GetSingleton<GameContainer>();
			var playersData = container.PlayersData;

			var teamMembers = new List<Quantum.PlayerMatchData>();
			for (int i = 0; i < playersData.Length; i++)
			{
				var data = playersData[i];

				if (data.IsValid && data.Entity.IsAlive(f) && data.TeamId == localTeamId)
				{
					teamMembers.Add(data);
				}
			}

			return teamMembers;
		}

		private List<Quantum.PlayerMatchData> GetPlayerList(QuantumGame game, out int currentIndex)
		{
			var f = game.Frames.Verified;
			var players = new List<Pair<EntityRef, PlayerRef>>();
			var container = f.GetSingleton<GameContainer>();

			var validPlayers = GetLivingTeamMembers(game);
			if (validPlayers.Count == 0)
			{
				for (int i = 0; i < container.PlayersData.Length; i++)
				{
					var data = container.PlayersData[i];
					if (data.IsValid && data.Entity.IsAlive(f))
					{
						validPlayers.Add(data);
					}
				}
			}

			currentIndex = -1;
			foreach (var data in validPlayers)
			{
				players.Add(new Pair<EntityRef, PlayerRef>(data.Entity, data.Player));

				if (_spectatedPlayer.Value.Entity == data.Entity)
				{
					currentIndex = players.Count - 1;
				}
			}

			return validPlayers;
		}
	}
}