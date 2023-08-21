using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			return new Member()
			{
				MemberEntity = LocalEntityKey(),
				MemberData = new()
				{
					{DisplayNameMemberProperty, _appDataProvider.GetDisplayName()},
					{LevelProperty, _playerDataProvider.Level.Value.ToString()},
					{TrophiesProperty, _playerDataProvider.Trophies.Value.ToString()}
				}
			};
		}

		[CanBeNull]
		private PartyMember LocalPartyMember()
		{
			return Members.FirstOrDefault(m => m.Local);
		}

		private PartyMember ToPartyMember(MemberToMerge m, bool leader = false)
		{
			return FromData(m.memberEntity.Id, m.memberData, leader);
		}

		private PartyMember FromData(string id, Dictionary<string, string> data, bool leader)
		{
			var member = new PartyMember(
				playfabID: id,
				displayName: data?[DisplayNameMemberProperty],
				trophies: 0,
				bppLevel: 0,
				local: Local().EntityId == id,
				leader: leader,
				ready: false,
				rawProperties: new()
			);
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
			if (data.ContainsKey(LevelProperty) && uint.TryParse(data[LevelProperty], out var bppLevelInt))
			{
				member.BPPLevel = bppLevelInt;
				updated = true;
			}

			if (data.ContainsKey(TrophiesProperty) && uint.TryParse(data[TrophiesProperty], out var trophiesInt))
			{
				member.Trophies = trophiesInt;
				updated = true;
			}

			if (data.ContainsKey(ReadyMemberProperty) && bool.TryParse(data[ReadyMemberProperty], out var readyBool))
			{
				member.Ready = readyBool;
				updated = true;
			}

			if (data.TryGetValue(DisplayNameMemberProperty, out var displayName))
			{
				member.DisplayName = displayName;
				updated = true;
			}

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
			if (Members == null || Members.Count == 0) return "";
			return string.Join(",", Members.ReadOnlyList.Select(m => m.PlayfabID));
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

			if (_errorMapping.TryGetValue(code, out var partyError))
			{
				return partyError;
			}

			if (code == PlayFabErrorCode.LobbyBadRequest)
			{
				if (ex.Error.ErrorMessage == "Cannot get lobby details since user is not lobby owner or member")
				{
					return PartyErrors.TryingToGetDetailsOfNonMemberParty;
				}
			}

			return PartyErrors.Unknown;
		}
	}
}