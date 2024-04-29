using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
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
		public Color? GetTeamMemberColor(PlayerProperties color);

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
	public class TeamService : ITeamService
	{
		private IRoomService _roomService;

		public TeamService(IRoomService roomService)
		{
			_roomService = roomService;
			roomService.OnJoinedRoom += OnJoinedRoom;
		}


		public Color? GetTeamMemberColor(EntityRef e)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning(false)) return null;
			if (!QuantumRunner.Default.PredictedFrame().TryGet<TeamMember>(e, out var member)) return null;
			return TeamConstants.Colors[Mathf.Min(member.Color, TeamConstants.Colors.Length - 1)];
		}

		public Color? GetTeamMemberColor(PlayerProperties properties)
		{
			return TeamConstants.Colors[Mathf.Min(properties.ColorIndex.Value, TeamConstants.Colors.Length - 1)];
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


		private void OnJoinedRoom()
		{
			var localProperties = _roomService.CurrentRoom.LocalPlayerProperties;
			var team = localProperties.TeamId.Value;
			List<byte> usedIds = new ();
			foreach (var playersValue in _roomService.CurrentRoom.Players.Values)
			{
				if (playersValue.IsLocal) continue;
				var properties = _roomService.CurrentRoom.GetPlayerProperties(playersValue);
				if (properties.TeamId.Value == team && properties.ColorIndex.HasValue)
				{
					usedIds.Add(properties.ColorIndex.Value);
				}
			}

			byte max = (byte) (usedIds.Count > 0 ? usedIds.Max() + 1 : 1);
			for (byte i = 0; i <= max; i++)
			{
				if (!usedIds.Contains(i))
				{
					localProperties.ColorIndex.Value = i;
					break;
				}
			}
		}
	}
}