using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public unsafe struct QuantumPlayerMatchData
	{
		public string PlayerName;
		public uint PlayerRank;
		public int MapId;
		public string GameModeId;
		public PlayerMatchData Data;
		public bool IsBot;
		public int TeamId;
		public string AvatarUrl;
		public uint LeaderboardRank;

		public QuantumPlayerMatchData(Frame f, PlayerRef player) : this(f, f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[player])
		{
		}

		public QuantumPlayerMatchData(Frame f, PlayerMatchData data) : this()
		{
			var playerData = f.GetPlayerData(data.Player);

			IsBot = playerData == null;
			MapId = f.RuntimeConfig.MapId;
			GameModeId = f.RuntimeConfig.GameModeId;
			Data = data;
			PlayerName = playerData == null ? data.BotNameIndex.ToString() : playerData.PlayerName;
			TeamId = data.TeamId;
			AvatarUrl = playerData?.AvatarUrl;
			LeaderboardRank = playerData == null ? 0 : playerData.LeaderboardRank;
		}

		public override int GetHashCode()
		{
			int hash = 67;
			hash = hash * 31 + Data.GetHashCode();
			hash = hash * 31 + PlayerRank.GetHashCode();
			hash = hash * 31 + MapId.GetHashCode();
			hash = hash * 31 + PlayerName.GetHashCode();
			return hash;
		}
	}
}