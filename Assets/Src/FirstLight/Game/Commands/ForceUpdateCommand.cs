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
		
		/// <inheritdoc />
		public CommandAccessLevel AccessLevel => CommandAccessLevel.Admin;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			if (PlayerData != null)
			{
				CopyPropertiesShallow(PlayerData, dataProvider.GetData<PlayerData>());
			}
			if (RngData != null)
			{
				CopyPropertiesShallow(RngData, dataProvider.GetData<RngData>());
			}
			if (IdData != null)
			{
				CopyPropertiesShallow(IdData, dataProvider.GetData<IdData>());
			}
		}
		
		/// <summary>
		/// Copy properties from one model to another.
		/// Only a shallow copy.
		/// </summary>
		private static void CopyPropertiesShallow<T>(T source, T dest)
		{
			foreach (var property in typeof(T).GetProperties().Where(p => p.CanWrite))
			{
				property.SetValue(dest, property.GetValue(source, null), null);
			}
		}
	}
}