using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Quantum;
using Quantum.Systems;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.Match
{
	public interface IEntityVisibilityService
	{
		/// <summary>
		/// Checks if the local player can see something
		/// </summary>
		bool CanSpectatedPlayerSee(EntityRef entity);

		/// <summary>
		/// Checks if the given entity is in an invisibility area
		/// </summary>
		bool IsInInvisibilityArea(EntityRef entity);
	}

	/// <summary>
	/// This class handles showing/hiding player renderers inside and outside of visibility volumes based on various factors
	/// </summary>
	public class EntityVisibilityService : IEntityVisibilityService, MatchServices.IMatchService
	{
		private IGameServices _gameServices;
		private IMatchServices _matchServices;
		private HashSet<EntityRef> _waitingLoad = new();
		private HashSet<RenderersContainerProxyMonoComponent> _clientHidden = new();
		private readonly Color _inBushColor = new Color(110/255f, 150/255f, 110/255f, 1);

		public EntityVisibilityService(IMatchServices s, IGameServices gameServices)
		{
			_matchServices = s;
			_gameServices = gameServices;
			QuantumEvent.SubscribeManual<EventOnEnterVisibilityArea>(this, OnEnterVisibilityArea);
			QuantumEvent.SubscribeManual<EventOnLeaveVisibilityArea>(this, OnLeaveVisibilityArea);
			_gameServices.MessageBrokerService.Subscribe<EntityViewLoaded>(OnEntityViewLoad);
			_gameServices.MessageBrokerService.Subscribe<ItemEquippedMessage>(OnItemEquipped);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectateChange); 
		}
		
		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
			_gameServices.MessageBrokerService.Unsubscribe<EntityViewLoaded>(OnEntityViewLoad);
		}

		public VisibilityCheckResult CheckSpectatorVisibility(EntityRef entity)
		{
			var spectator = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;
			var result = VisibilityAreaSystem.CanEntityViewEntity(QuantumRunner.Default.Game.Frames.Verified, spectator, entity);;
			if (result.TargetArea.Area.IsValid && FeatureFlags.ALWAYS_TOGGLE_INVISIBILITY_AREAS)
			{
				result.CanSee = false;
			}
			return result;
		}
		
		public bool CanSpectatedPlayerSee(EntityRef entity)
		{
			if (!FeatureFlags.ALWAYS_TOGGLE_INVISIBILITY_AREAS && _matchServices.SpectateService.GetSpectatedEntity() == entity) return true;
			return CheckSpectatorVisibility(entity).CanSee;
		}

		private void OnItemEquipped(ItemEquippedMessage msg)
		{
			if (!CanSpectatedPlayerSee(msg.Character.EntityRef))
			{
				var rend = msg.Item.GetComponentInChildren<RenderersContainerMonoComponent>();
				rend?.SetEnabled(false);
			}
		}
		
		private void OnSpectateChange(SpectatedPlayer oldView, SpectatedPlayer newView)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning(false))
			{
				Log.Error("Tried to change spectator while quantum game was not running");
				return;
			}
			if (newView.Entity == QuantumRunner.Default.Game.GetLocalPlayerEntityRef()) return;
			if (!newView.Entity.IsValid || !oldView.Entity.IsValid) return;
			
			foreach (var hidden in _clientHidden)
			{
				if (!hidden.IsDestroyed()) Reset(hidden);
			}
			UpdateLocalPlayerViewOn(newView.Entity);
			var area = GetInvisibilityArea(newView.Entity);
			if (area.HasValue)
			{
				UpdateSpectatedArea(QuantumRunner.Default.Game.Frames.Verified, area.Value.Area);
			}
		}
		
		/// <summary>
		/// On some entities we do not attach the renderer directly on the object.
		/// We load it via addressables. That means an entity can load while being inside a visibility volume and
		/// wont have its visibility volume correctly triggered.
		/// </summary>
		private void OnEntityViewLoad(EntityViewLoaded load)
		{
			if (_waitingLoad.Contains(load.View.EntityRef))
			{
				UpdateLocalPlayerViewOn(load.View.EntityRef);
				_waitingLoad.Remove(load.View.EntityRef);
			}
		}

		private void OnEnterVisibilityArea(EventOnEnterVisibilityArea ev)
		{
			if (InterestedInAreaUpdate(ev.Entity))
			{
				UpdateSpectatedArea(ev.Game.Frames.Verified, ev.Area);
			}
			
			UpdateLocalPlayerViewOn(ev.Entity);
		}
		
		private void OnLeaveVisibilityArea(EventOnLeaveVisibilityArea ev)
		{
			if (InterestedInAreaUpdate(ev.Entity))
			{
				UpdateSpectatedArea(ev.Game.Frames.Verified, ev.Area);
			}
			UpdateLocalPlayerViewOn(ev.Entity);
		}

		/// <summary>
		/// Method controls who needs to receive updates from all entities in an area when leaving or entering.
		/// Local player when enters an area needs to view everyone inside the area.
		/// Same might apply if a teammate enters an area.
		/// </summary>
		private bool InterestedInAreaUpdate(EntityRef entity)
		{
			return IsSpectator(entity)
				|| TeamSystem.HasSameTeam(
					QuantumRunner.Default.Game.Frames.Verified, entity,
					QuantumRunner.Default.Game.GetLocalPlayerEntityRef());
		}

		/// <summary>
		/// When local player enters or leaves a area, updates everything inside the area
		/// to be visible or not
		/// </summary>
		private void UpdateSpectatedArea(Frame f, EntityRef area)
		{
			if (!f.TryGet<VisibilityArea>(area, out var areaComponent)) return;
			foreach (var entityRef in f.ResolveList(areaComponent.EntitiesIn))
			{
				if(!IsSpectator(entityRef)) UpdateLocalPlayerViewOn(entityRef);
			}
		}

		/// <summary>
		/// Updates if the local player should or should not see the given entity.
		/// Should be triggered whenever this specific entity leaves or enter a visibility area
		/// </summary>
		private void UpdateLocalPlayerViewOn(EntityRef towardsEntity)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(towardsEntity, out var view))
			{
				return;
			}
			
			var renderer = FindRenderer(view.gameObject);
			if(renderer == null)
			{
				_waitingLoad.Add(towardsEntity);
				return;
			}
			
			var visibility = CheckSpectatorVisibility(towardsEntity);
			FLog.Verbose($"[EntityVisibility] Setting {view.EntityRef} render to {visibility.CanSee}");
			renderer.SetEnabled(visibility.CanSee);
			if(view.gameObject.TryGetComponent<PlayerCharacterMonoComponent>( out var PCC))
			{
				PCC.SwitchShadowVisibility(visibility.CanSee);
			}

			if (!visibility.CanSee) _clientHidden.Add(renderer);
			else _clientHidden.Remove(renderer);

			if (IsSpectator(towardsEntity))
			{
				var f = QuantumRunner.Default.Game.Frames.Verified;
				
				if (visibility.TargetArea.Area.IsValid && f.TryGet<VisibilityArea>(visibility.TargetArea.Area, out var visibilityArea))
				{
					if (visibilityArea.AreaType == VisibilityAreaType.Bush)
					{
						renderer.SetColor(_inBushColor);
					}
				}
				else
				{
					renderer.ResetColor();
				}
			}
			
			_gameServices.MessageBrokerService.Publish(new LocalPlayerEntityVisibilityUpdate()
			{
				Entity = towardsEntity,
				CanSee = visibility.CanSee
			});
		}

		public bool IsSpectator(EntityRef e)
		{
			return _matchServices.SpectateService.SpectatedPlayer?.Value.Entity == e;
		}

		public bool IsInInvisibilityArea(EntityRef entity)
		{
			return QuantumRunner.Default.Game.Frames.Verified.Has<InsideVisibilityArea>(entity);
		}

		private void Reset(RenderersContainerProxyMonoComponent renderer)
		{
			renderer.ResetColor();
			renderer.SetEnabled(true);
		}
		
		public InsideVisibilityArea? GetInvisibilityArea(EntityRef entity)
		{
			if (!QuantumRunner.Default.Game.Frames.Verified.TryGet<InsideVisibilityArea>(entity, out var area))
			{
				return null;
			}
			return area;
		}

		[CanBeNull]
		private RenderersContainerProxyMonoComponent FindRenderer(GameObject view)
		{
			if (!view.TryGetComponent<RenderersContainerProxyMonoComponent>(out var viewBase))
			{
				return view.GetComponentInChildren<RenderersContainerProxyMonoComponent>(true);
			}
			return viewBase;
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			foreach (var hidden in _clientHidden)
			{
				if (!hidden.IsDestroyed()) Reset(hidden);
			}
		}
		
		public void OnMatchStarted(QuantumGame game, bool isReconnect){ }
	}
}