using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public struct ConsumeWeb3VoucherCommand : IGameCommand
	{
		public Guid VoucherId;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;
		
		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var d = ctx.Data.GetData<Web3PlayerData>();
			var voucherId = VoucherId;
			for (var x = 0; x < d.Vouchers.Count; x++)
			{
				var v = d.Vouchers[x];
				if (v.VoucherId == voucherId)
				{
					d.Vouchers.RemoveAt(x);
					d.Version++;
					ctx.Services.MessageBrokerService().Publish(new VoucherConsumedMessage()
					{
						Voucher = v
					});
					return UniTask.CompletedTask;
				}
			}
			throw new Exception("Voucher not found");
		}
	}
}