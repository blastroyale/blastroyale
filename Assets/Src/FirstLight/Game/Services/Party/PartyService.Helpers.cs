using System;
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
				Type = Local().EntityType
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
					{LevelProperty, _playerDataProvider.PlayerInfo.Level.ToString()},
					{TrophiesProperty, _playerDataProvider.PlayerInfo.TotalTrophies.ToString()}
				}
			};
		}

		[CanBeNull]
		private PartyMember LocalPartyMember()
		{
			return Members.FirstOrDefault(m => m.Local);
		}

		private PartyMember ToPartyMember(Lobby l, Member m)
		{
			// Parse BPP
			if (!uint.TryParse(m.MemberData[LevelProperty], out var bppLevelInt))
			{
				bppLevelInt = 0;
			}

			// Parse Trophies
			if (!uint.TryParse(m.MemberData[TrophiesProperty], out var trophiesInt))
			{
				trophiesInt = 0;
			}

			// Parse Ready Status
			if (!m.MemberData.ContainsKey(ReadyProperty) || !bool.TryParse(m.MemberData[ReadyProperty], out var readyBool))
			{
				readyBool = false;
			}


			return new PartyMember(
				playfabID: m.MemberEntity.Id,
				displayName: m.MemberData[DisplayNameMemberProperty],
				trophies: trophiesInt,
				bppLevel: bppLevelInt,
				local: Local().EntityId == m.MemberEntity.Id,
				leader: l.Owner.Id == m.MemberEntity.Id,
				ready: readyBool
			);
		}

		private String GenerateCode()
		{
			var code = new StringBuilder();
			for (int i = 0; i < CodeDigits; i++)
			{
				int rndIndex = Random.Range(0, JoinCodeAllowedCharacters.Length);
				code.Append(JoinCodeAllowedCharacters[rndIndex]);
			}

			return code.ToString();
		}

		private string MembersAsString()
		{
			if (_lobby?.Members == null || _lobby.Members.Count == 0) return "";
			return string.Join(",", _lobby?.Members?.Select(m => m.MemberEntity.Id));
		}


		private String NormalizeCode(string code)
		{
			string temp = code.ToUpper();
			foreach (var (from, to) in CharCodeReplaces)
			{
				temp = temp.Replace(from, to);
			}

			return temp;
		}


		private void HandleException(Exception ex)
		{
			Debug.LogException(ex);
			if (ex is PartyException) throw ex;
			var err = PartyErrors.Unknown;
			if (ex is WrappedPlayFabException playfabEx)
			{
				err = ConvertErrors(playfabEx);
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

			return PartyErrors.Unknown;
		}
	}
}