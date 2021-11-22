using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public unsafe struct QuantumPlayerMatchData
	{
		public string PlayerName;
		public bool IsBot;
		public bool IsLocalPlayer;
		public PlayerMatchData Data;

		public QuantumPlayerMatchData(Frame f, PlayerRef player) : this(f, f.GetSingleton<GameContainer>().PlayersData[player])
		{
		}

		public QuantumPlayerMatchData(Frame f, PlayerMatchData data) : this()
		{
			IsBot = f.TryGet<BotCharacter>(data.Entity, out var deadBot);
			IsLocalPlayer = f.Context.IsLocalPlayer(data.Player);
			PlayerName = IsBot ? deadBot.BotNameIndex.ToString() : f.GetPlayerData(data.Player).PlayerName; 
			Data = data;
		}
	}
}