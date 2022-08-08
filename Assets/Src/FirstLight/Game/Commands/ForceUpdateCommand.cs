using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Forces the game state to become the given state.
	/// Requires admin permission on server.
	/// </summary>
	public struct ForceUpdateCommand : IGameCommand
	{
		public PlayerData PlayerData;
		public RngData RngData;
		public IdData IdData;
		public EquipmentData EquipmentData;
		
		/// <inheritdoc />
		public CommandAccessLevel AccessLevel => CommandAccessLevel.Admin;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			if (PlayerData != null)
			{
				PlayerData.CopyPropertiesShallowTo(dataProvider.GetData<PlayerData>());
			}
			if (RngData != null)
			{
				RngData.CopyPropertiesShallowTo(dataProvider.GetData<RngData>());
			}
			if (IdData != null)
			{
				IdData.CopyPropertiesShallowTo(dataProvider.GetData<IdData>());
			}
			if (EquipmentData != null)
			{
				EquipmentData.CopyPropertiesShallowTo(dataProvider.GetData<EquipmentData>());
			}
		}
	}
}