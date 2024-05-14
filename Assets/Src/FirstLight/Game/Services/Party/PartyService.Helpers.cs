using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using I2.Loc;
using JetBrains.Annotations;
using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Services.Party
{
	public partial class PartyService
	{
		private PlayFabAuthenticationContext Local()
		{
			return PlayFabSettings.staticPlayer;
		}

		private EntityKey LocalEntityKey()
		{
			return new EntityKey()
			{
				Id = Local().EntityId,
				Type = PlayFabConstants.TITLE_PLAYER_ENTITY_TYPE
			};
		}

		private Member CreateLocalMember()
		{
			var collectionDataProvider = MainInstaller.ResolveData().CollectionDataProvider;
			var playerDataProvider = MainInstaller.ResolveData().PlayerDataProvider;
			return new Member()
			{
				MemberEntity = LocalEntityKey(),
				MemberData = new ()
				{
					{PartyMember.DISPLAY_NAME_MEMBER_PROPERTY, _appDataProvider.GetDisplayName()},
					{PartyMember.TROPHIES_PROPERTY, playerDataProvider.Trophies.Value.ToString()},
					{PartyMember.READY_MEMBER_PROPERTY, "notready"},
					{PartyMember.CHARACTER_SKIN_PROPERTY, collectionDataProvider.GetEquipped(CollectionCategories.PLAYER_SKINS).Id.ToString()},
					{PartyMember.MELEE_SKIN_PROPERTY, collectionDataProvider.GetEquipped(CollectionCategories.MELEE_SKINS).Id.ToString()},
					{PartyMember.PROFILE_MASTER_ID, PlayFabSettings.staticPlayer.PlayFabId},
				}
			};
		}

		[CanBeNull]
		private PartyMember LocalPartyMember()
		{
			return _members.FirstOrDefault(m => m.Local);
		}

		private PartyMember ToPartyMember(MemberToMerge m, bool leader = false)
		{
			return FromData(m.memberEntity.Id, m.memberData, leader);
		}

		private PartyMember FromData(string id, Dictionary<string, string> data, bool leader)
		{
			var member = new PartyMember()
			{
				PlayfabID = id,
				Leader = leader,
				RawProperties = new Dictionary<string, string>()
			};
			if (data != null)
			{
				MergeData(member, data);
			}

			return member;
		}

		private bool MergeData(PartyMember member, Dictionary<string, string> data)
		{
			if (data == null) return false;
			var updated = false;

			foreach (var (key, value) in data)
			{
				if (member.RawProperties.TryGetValue(key, out var currentValue))
				{
					if (currentValue == value)
					{
						continue;
					}
				}

				member.RawProperties[key] = value;
				updated = true;
			}

			return updated;
		}

		private PartyMember ToPartyMember(Lobby l, Member m)
		{
			return FromData(m.MemberEntity.Id, m.MemberData, m.MemberEntity.Id == l.Owner.Id);
		}

		private string MembersAsString()
		{
			if (_members == null || _members.Count == 0) return "";
			return string.Join(",", _members.ReadOnlyList.Select(m => m.PlayfabID));
		}


		private void HandleException(Exception ex)
		{
			if (ex is PartyException) throw ex;
			var err = PartyErrors.Unknown;
			if (ex is WrappedPlayFabException playfabEx)
			{
				err = ConvertErrors(playfabEx);
			}

			if (err == PartyErrors.TryingToGetDetailsOfNonMemberParty)
			{
				ResetState();
			}

			throw new PartyException(ex, err);
		}


		private PartyErrors ConvertErrors(WrappedPlayFabException ex)
		{
			var code = ex.Error.Error;

			if (_customErrorMessagesMapping.TryGetValue(code, out var mapping))
			{
				if (mapping.TryGetValue(ex.Error.ErrorMessage, out var transformedCode))
				{
					return transformedCode;
				}
			}

			if (_errorMapping.TryGetValue(code, out var partyError))
			{
				return partyError;
			}

			return PartyErrors.Unknown;
		}
	}
}