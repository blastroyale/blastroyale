using System;

namespace FirstLight.Game.Services.Party
{
	public enum PartyErrors
	{
		BannedFromParty,
		NoPermission,
		NoParty,
		PartyFull,
		AlreadyInParty,
		PartyNotFound,
		DifferentGameVersion,
		PartyUsingOtherServer,
		MemberNotFound,
		ConnectionError,
		TryingToGetDetailsOfNonMemberParty,
		UserIsNotMember,
		Unknown
	}

	public class PartyException : Exception
	{
		public PartyErrors Error { get; }


		public PartyException(PartyErrors error) : base(error.ToString())
		{
			Error = error;
		}

		public PartyException(Exception innerException, PartyErrors error) : base(error.ToString(), innerException)
		{
			Error = error;
		}
	}
}