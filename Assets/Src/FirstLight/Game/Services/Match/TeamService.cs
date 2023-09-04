using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Quantum.Systems;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.Match
{
	public static class TeamConstants
	{
		public static readonly Color [] Colors = {
			new (15/255f, 145/255f, 70/255f), 
			new (216/255f, 75/255f, 121/255f), 
			new (93/255f, 127/255f, 239/255f),
			new (144/255f,93/255f, 239/255f), 
		};
	}
	
	public interface ITeamService
	{
		/// <summary>
		/// Gets the current team color of the current entity.
		/// Returns null in case no color is assigned.
		/// </summary>
		public Color? GetTeamMemberColor(EntityRef entity);

		/// <summary>
		/// Gets team of a given entity
		/// </summary>
		public int GetTeam(EntityRef entity);

		/// <summary>
		/// Checks if the entity is from the same team as the spectated player
		/// </summary>
		public bool IsSameTeamAsSpectator(EntityRef entity);
	}

	/// <summary>
	/// Match service for team related handling
	/// </summary>
	public class TeamService : ITeamService, MatchServices.IMatchService
	{
		private IGameServices _gameServices;
		private IMatchServices _matchServices;
		private IEntityViewUpdaterService _entityViewUpdater;

		public TeamService(IGameServices gameServices, IMatchServices matchServices)
		{
			_matchServices = matchServices;
			_gameServices = gameServices;
		}

		public Color? GetTeamMemberColor(EntityRef e)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return null;
			if (!QuantumRunner.Default.PredictedFrame().TryGet<TeamMember>(e, out var member)) return null;
			return TeamConstants.Colors[Mathf.Max(member.TeamIndex, TeamConstants.Colors.Length-1)];
		}

		public int GetTeam(EntityRef e)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return -1;
			if (!QuantumRunner.Default.Game.Frames.Predicted.TryGet<TeamMember>(e, out var target)) return -1;
			return target.TeamId;
		}

		public bool IsSameTeamAsSpectator(EntityRef entity)
		{
			var team = GetTeam(entity);
			return team > 0 && team == GetTeam(_matchServices.SpectateService.GetSpectatedEntity());
		}

		public void Dispose()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected) { }

		public void OnMatchStarted(QuantumGame game, bool isReconnect) { }
	}
}