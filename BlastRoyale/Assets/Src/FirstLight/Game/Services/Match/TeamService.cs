using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using Photon.Realtime;
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
		public Color? GetTeamMemberColor(Player player);

		/// <summary>
		/// Gets team of a given entity
		/// </summary>
		public byte GetTeamMemberColorIndex(Player player);

		/// <summary>
		/// Get the team id for a given player
		/// </summary>
		public string GetTeamForPlayer(Player player);

		/// <summary>
		/// Checks if the entity is from the same team as the spectated player
		/// </summary>
		public bool IsSameTeamAsSpectator(EntityRef entity);
	}

	/// <summary>
	/// Match service for team related handling
	/// </summary>
	public class TeamService : ITeamService
	{
		private IRoomService _roomService;

		public TeamService(IRoomService roomService)
		{
			_roomService = roomService;
			//roomService.OnJoinedRoom += BeforeCustomGameStarts;
		}

		public Color? GetTeamMemberColor(EntityRef e)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning(false)) return null;
			if (!QuantumRunner.Default.PredictedFrame().TryGet<TeamMember>(e, out var member)) return null;
			return TeamConstants.Colors[Mathf.Min(member.Color, TeamConstants.Colors.Length - 1)];
		}

		public Color? GetTeamMemberColor(Player player)
		{
			var index = GetTeamMemberColorIndex(player);
			return TeamConstants.Colors[Mathf.Min(index, TeamConstants.Colors.Length - 1)];
		}

		public byte GetTeamMemberColorIndex(Player player)
		{
			var room = _roomService.CurrentRoom;

			var playerProps = room.GetPlayerProperties(player);
			return playerProps.ColorIndex.HasValue ? playerProps.ColorIndex.Value : (byte) 0;
		}

		public int GetTeam(EntityRef e)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning(false)) return -1;
			if (!QuantumRunner.Default.Game.Frames.Predicted.TryGet<TeamMember>(e, out var target)) return -1;
			return target.TeamId;
		}

		public bool IsSameTeamAsSpectator(EntityRef entity)
		{
			var matchServices = MainInstaller.ResolveMatchServices();
			var team = GetTeam(entity);
			return team > 0 && team == GetTeam(matchServices.SpectateService.GetSpectatedEntity());
		}

		public string GetTeamForPlayer(Player player)
		{
			var room = _roomService.CurrentRoom;
			return room.GetPlayerProperties(player).TeamId.Value;
		}
	}
}