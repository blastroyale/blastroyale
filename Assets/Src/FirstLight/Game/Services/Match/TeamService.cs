using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.Match
{
	public static class TeamConstants
	{
		public static readonly Color[] Colors =
		{
			new (0xFF / 255f, 0xD8 / 255f, 0x00 / 255f),
			new (0xFF / 255f, 0x51 / 255f, 0x7E / 255f),
			new (0x8C / 255f, 0x4D / 255f, 0xFF / 255f),
			new (0x18 / 255f, 0xD0 / 255f, 0xC9 / 255f),
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
			return TeamConstants.Colors[Mathf.Min(member.TeamIndex, TeamConstants.Colors.Length - 1)];
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

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
		}
	}
}