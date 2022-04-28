using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.CloudScriptModels;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Calculates and updates the change in player's trophies.
	/// </summary>
	public struct UpdatePlayerTrophiesCommand : IGameCommand
	{
		public QuantumPlayerMatchData[] Players;
		public uint LocalPlayerRank;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.MatchLogic.UpdateTrophies(Players, LocalPlayerRank);
		}
	}
}