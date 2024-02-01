using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Forces the game state to become the given state.
	/// Requires admin permission on server.
	/// </summary>
	public struct ForceUpdateCommand : IGameCommand
	{
		public Dictionary<Type, string> Data;

		public ForceUpdateCommand(Dictionary<Type, object> data)
		{
			Data = data.ToDictionary(key => key.Key, key => ModelSerializer.Serialize(key.Value).Value);
		}

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Admin;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			foreach (var (type, value) in Data)
			{
				var convertedValue = ModelSerializer.Deserialize(type, value);
				var currentData = ctx.Data.GetData(type);
				convertedValue.CopyPropertiesShallowTo(currentData);
			}
			return UniTask.CompletedTask;
		}
	}
}