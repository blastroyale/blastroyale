using System;
using Photon.Deterministic;

namespace Quantum 
{

	unsafe partial class RuntimePlayer
	{
		public string PlayerId;
		public string UnityId;
		public string PlayerName;
		public GameId[] Cosmetics;
		public uint PlayerLevel;
		public uint PlayerTrophies;
		public uint LeaderboardRank;
		public FPVector2 NormalizedSpawnPosition;
		public string PartyId = string.Empty;
		public string AvatarUrl;
		public bool UseBotBehaviour;
		public byte TeamColor;
		public GameId DeathFlagID;

		partial void SerializeUserData(BitStream stream)
		{
			stream.Serialize(ref PlayerId);
			stream.Serialize(ref UnityId); // this makes me saaaaad :cat_crying:
			stream.Serialize(ref PlayerName);
			stream.Serialize(ref PlayerLevel);
			stream.Serialize(ref PlayerTrophies);
			stream.Serialize(ref NormalizedSpawnPosition);
			stream.Serialize(ref PartyId);
			stream.Serialize(ref AvatarUrl);
			stream.Serialize(ref UseBotBehaviour);
			stream.Serialize(ref TeamColor);
			stream.Serialize(ref LeaderboardRank);
			stream.SerializeArrayLength(ref Cosmetics);
			for (var i = 0; i < Cosmetics.Length; i++)
			{
				var skinId = (int)Cosmetics[i];
				stream.Serialize(ref skinId);
				Cosmetics[i] = (GameId)skinId;
			}

			var deathFlagID = (int)DeathFlagID;
			stream.Serialize(ref deathFlagID);
			DeathFlagID = (GameId) deathFlagID;
		}
	}
}
