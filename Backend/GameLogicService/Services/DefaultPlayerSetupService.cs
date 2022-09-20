using System;
using System.Collections.Generic;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;


namespace Backend.Game.Services
{
	/// <inheritdoc />
	public class DefaultPlayerSetupService : IPlayerSetupService
	{
		/// <inheritdoc />
		public ServerState GetInitialState(string playFabId)
		{
			return new ServerState();
		}

		/// <inheritdoc />
		public bool IsSetup(ServerState state)
		{
			return state != null;
		}
	}
}


