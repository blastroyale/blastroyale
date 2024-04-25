using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using Quantum;

namespace Src.FirstLight.Server.ServerServices
{
	/// <inheritdoc />
	public class BlastRoyalePlayerSetup : IPlayerSetupService
	{
		private static readonly List<GameId> _initialSkins = new List<GameId>
		{
			GameId.PlayerSkinBrandMale
		};
		
		private static IConfigsProvider _configsProvider;

		public BlastRoyalePlayerSetup(IConfigsProvider configsProvider)
		{
			_configsProvider = configsProvider;
		}

		/// <inheritdoc />
		public ServerState GetInitialState(string playFabId)
		{
			var rngData = SetupInitialRngData(playFabId.GetHashCode());
			var serverState = new ServerState();
			serverState.UpdateModel(new PlayerData());
			serverState.UpdateModel(new LiveopsData());
			serverState.UpdateModel(new CollectionData());
			serverState.UpdateModel(new IdData());
			serverState.UpdateModel(rngData);
			serverState.UpdateModel(new EquipmentData());
			return serverState;
		}

		/// <inheritdoc />
		public bool IsSetup(ServerState state)
		{
			if (state.Count == 0)
			{
				return false;
			}

			if (!state.Has<PlayerData>())
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Initializes player RngData
		/// </summary>
		private static RngData SetupInitialRngData(int seed)
		{
			return new RngData
			{
				Count = 0,
				Seed = seed,
				State = RngUtils.GenerateRngState(seed)
			};
		}
	}
}

