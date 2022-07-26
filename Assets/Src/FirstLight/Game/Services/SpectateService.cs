using System.Collections.Generic;
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
		IObservableFieldReader<ObservedPlayer> SpectatedPlayer { get; }

		/// <summary>
		/// Starts spectating the next player.
		/// </summary>
		public void SwipeLeft();

		/// <summary>
		/// Starts spectating the previous player.
		/// </summary>
		public void SwipeRight();
	}

	public struct ObservedPlayer
	{
		public EntityRef Entity;
		public PlayerRef Player;
		public Transform Transform;

		public ObservedPlayer(EntityRef entity, PlayerRef player, Transform transform)
		{
			Entity = entity;
			Player = player;
			Transform = transform;
		}
	}

	public class SpectateService : ISpectateService, IMatchService
	{
		public IObservableFieldReader<ObservedPlayer> SpectatedPlayer => _spectatedPlayer;

		private readonly IEntityViewUpdaterService _entityViewUpdaterService;
		private readonly IGameNetworkService _networkService;

		private readonly FP _playerVisionRange;

		private readonly IObservableField<ObservedPlayer> _spectatedPlayer = new ObservableField<ObservedPlayer>();

		public SpectateService(IEntityViewUpdaterService entityViewUpdaterService, IGameNetworkService networkService,
		                       IConfigsProvider configsProvider)
		{
			_entityViewUpdaterService = entityViewUpdaterService;
			_networkService = networkService;

			_playerVisionRange = configsProvider.GetConfig<QuantumGameConfig>().PlayerVisionRange;
		}

		public void OnMatchStarted()
		{
			if (_networkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				SwipeRight();
			}
			else
			{
				var game = QuantumRunner.Default.Game;
				var f = QuantumRunner.Default.Game.Frames.Verified;
				var gameContainer = f.GetSingleton<GameContainer>();
				var playersData = gameContainer.PlayersData;

				var localPlayer = playersData[game.GetLocalPlayers()[0]];

				SetSpectatedEntity(localPlayer.Entity, localPlayer.Player);
			}

			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnQuantumUpdateView);

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
		}

		private void OnQuantumUpdateView(CallbackUpdateView callback)
		{
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
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			var selected = players[currentIndex - 1 >= 0 ? currentIndex - 1 : players.Count - 1];
			SetSpectatedEntity(selected.Key, selected.Value);
		}

		public void SwipeRight()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			var selected = players[currentIndex + 1 < players.Count ? currentIndex + 1 : 0];
			SetSpectatedEntity(selected.Key, selected.Value);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityDead == _spectatedPlayer.Value.Entity)
			{
				if (callback.EntityDead == callback.EntityKiller)
				{
					SetSpectatedEntity(callback.EntityLeader, callback.PlayerLeader);
				}
				else
				{
					SetSpectatedEntity(callback.EntityKiller, callback.PlayerKiller);
				}
			}
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			SetSpectatedEntity(callback.Entity, callback.Player);
		}

		private void SetSpectatedEntity(EntityRef entity, PlayerRef player)
		{
			if (_spectatedPlayer.Value.Entity == entity) return;

			var transform = _entityViewUpdaterService.GetManualView(entity).transform;
			_spectatedPlayer.Value = new ObservedPlayer(entity, player, transform);
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
				}

				if (_spectatedPlayer.Value.Entity == data.Entity)
				{
					currentIndex = players.Count - 1;
				}
			}

			return players;
		}
	}
}