using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public unsafe struct QuantumPlayerMatchData
	{
		public string PlayerName;
		public bool IsLocalPlayer;
		public uint PlayerRank;
		public PlayerMatchData Data;

		public QuantumPlayerMatchData(Frame f, PlayerRef player) : this(f, f.GetSingleton<GameContainer>().PlayersData[player])
		{
		}

		public QuantumPlayerMatchData(Frame f, PlayerMatchData data) : this()
		{
			IsLocalPlayer = f.Context.IsLocalPlayer(data.Player);
			PlayerName = data.IsBot ? data.BotNameIndex.ToString() : f.GetPlayerData(data.Player).PlayerName; 
			Data = data;
		}
	}
}