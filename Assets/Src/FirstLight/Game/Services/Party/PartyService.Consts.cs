using System.Collections.Generic;
using PlayFab;

namespace FirstLight.Game.Services.Party
{
	public partial class PartyService
	{
		private const string CodeSearchProperty = "string_key1";
		private const string LobbyCommitProperty = "string_key2";
		private const string DisplayNameMemberProperty = "display_name";
		private const string LevelProperty = "bpp_level";
		private const string TrophiesProperty = "trophies";
		private const int CodeDigits = 4;
		private const int MaxMembers = 5;
		private const string JoinCodeAllowedCharacters = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private static readonly List<string> _memberRemovedReasons = new()
		{
			"MemberRemoved",
			"MemberRemovedPermanent"
		};

		private static readonly Dictionary<PlayFabErrorCode, PartyErrors> _errorMapping = new()
		{
			{ PlayFabErrorCode.LobbyDoesNotExist, PartyErrors.PartyNotFound },
			{ PlayFabErrorCode.LobbyPlayerAlreadyJoined, PartyErrors.AlreadyInParty },
			{ PlayFabErrorCode.LobbyMemberCannotRejoin, PartyErrors.BannedFromParty },
			{ PlayFabErrorCode.LobbyMemberIsNotOwner, PartyErrors.NoPermission },
			{ PlayFabErrorCode.LobbyPlayerNotPresent, PartyErrors.MemberNotFound },
			{ PlayFabErrorCode.LobbyCurrentPlayersMoreThanMaxPlayers, PartyErrors.PartyFull },
			{ PlayFabErrorCode.ConnectionError, PartyErrors.ConnectionError }
		};
	}
}