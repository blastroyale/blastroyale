using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public struct UpdateWeb3DataCommand : IGameCommand
	{
		public string PlayerWallet;
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;
		
		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var d = ctx.Data.GetData<Web3PlayerData>();
			d.Wallet = PlayerWallet;
			return UniTask.CompletedTask;
		}
	}
}