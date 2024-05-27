using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
namespace FirstLight.Game.MonoComponent.Match
{
	
	/// <summary>
	/// Old building visibility volume to be stored on players
	/// </summary>
	[System.Obsolete]
	public class PlayerBuildingVisibility
	{
		public HashSet<VisibilityVolumeMonoComponent> CollidingVisibilityVolumes = new ();

		public bool IsInLegacyVisibilityVolume() => CollidingVisibilityVolumes.Count > 0;
		public bool IsInSameLegacyVolumeAsSpectator()
		{
			return CollidingVisibilityVolumes.Any(visVolume => visVolume.VolumeHasSpectatedPlayer());
		}
	}
	
	/// <summary>
	/// This class handles showing/hiding player renderers inside and outside of visibility volumes based on various factors
	/// This is deprecated in favor of EntityVisibilityService
	/// </summary>
	[System.Obsolete]
	public class VisibilityVolumeMonoComponent : MonoBehaviour
	{
		[SerializeField] private bool _occludeAudio = true;
		
		private IGameServices _services;
		private IMatchServices _matchServices;
		private IEntityViewUpdaterService _entityViewUpdater;
		private Dictionary<EntityRef, PlayerCharacterViewMonoComponent> _currentlyCollidingPlayers;
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_currentlyCollidingPlayers = new Dictionary<EntityRef, PlayerCharacterViewMonoComponent>();
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			_services.MessageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnded);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnPlayerDead);
		}
		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		private void OnMatchEnded(MatchEndedMessage callback)
		{
			_currentlyCollidingPlayers.Clear();
		}
		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (_currentlyCollidingPlayers.ContainsKey(callback.Entity))
			{
				_currentlyCollidingPlayers.Remove(callback.Entity);
				CheckUpdateAllVisiblePlayers();
			}
		}
		/// <summary>
		/// Requests to check if this volume contains 
		/// </summary>
		public bool VolumeHasSpectatedPlayer()
		{
			return _currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
		}
		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			_currentlyCollidingPlayers.TryAdd(player.EntityRef, player);
			player.BuildingVisibility.CollidingVisibilityVolumes.Add(this);
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				CheckUpdateAllVisiblePlayers();
			}
			else if (player.BuildingVisibility.CollidingVisibilityVolumes.Count == 1)
			{
				CheckUpdateOneVisiblePlayer(player);
			}
			if (_occludeAudio && player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity &&
				player.BuildingVisibility.CollidingVisibilityVolumes.Count == 1)
			{
				_services.AudioFxService.TransitionAudioMixer(GameConstants.Audio.MIXER_INDOOR_SNAPSHOT_ID, GameConstants.Audio.MIXER_OCCLUSION_TRANSITION_SECONDS);
			}
		}
		private void OnTriggerExit(Collider other)
		{
			if (!other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			player.BuildingVisibility.CollidingVisibilityVolumes.Remove(this);
			// If ALL instances of this GO vis volume have been removed from player, only then the player is considered
			// not inside the volume anymore
			if (!player.BuildingVisibility.CollidingVisibilityVolumes.Contains(this))
			{
				_currentlyCollidingPlayers.Remove(player.EntityRef);
			}
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				CheckUpdateAllVisiblePlayers();
			}
			else if (player.BuildingVisibility.CollidingVisibilityVolumes.Count == 0)
			{
				CheckUpdateOneVisiblePlayer(player);
			}
			
			if (_occludeAudio && player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity &&
				player.BuildingVisibility.CollidingVisibilityVolumes.Count == 0)
			{
				_services.AudioFxService.TransitionAudioMixer(GameConstants.Audio.MIXER_MAIN_SNAPSHOT_ID, GameConstants.Audio.MIXER_OCCLUSION_TRANSITION_SECONDS);
			}
		}
		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateAllVisiblePlayers();
		}
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync) return;
			CheckUpdateAllVisiblePlayers();
		}
		private void CheckUpdateAllVisiblePlayers()
		{
			var spectatedPlayerWithinVolume =
				_currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			// Needed because players can get killed while being disconnected
			var destroyedPlayers = new List<EntityRef>();
			foreach (var player in _currentlyCollidingPlayers)
			{
				if (player.Value == null)
				{
					destroyedPlayers.Add(player.Key);
					continue;
				}
				player.Value.SetRenderContainerVisible(spectatedPlayerWithinVolume);
			}
			foreach (var entity in destroyedPlayers)
			{
				_currentlyCollidingPlayers.Remove(entity);
			}
		}
		private void CheckUpdateOneVisiblePlayer(PlayerCharacterViewMonoComponent player)
		{
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}
			var spectatedPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			var otherPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(player.EntityRef);
			var canSee = (spectatedPlayerWithinVolume == otherPlayerWithinVolume) ||
				(spectatedPlayerWithinVolume);
			FLog.Verbose($"[Visibility Volume] Setting {player.gameObject.name} visibility to {canSee}");
			player.SetRenderContainerVisible(canSee);
		}
	}
}