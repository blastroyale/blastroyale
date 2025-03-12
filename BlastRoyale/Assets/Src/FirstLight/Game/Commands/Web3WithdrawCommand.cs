using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// "Withdraws" a web3 currency - means it signs and transforms it in a voucher.
	/// The signature is only valid on server client ones can be discarded.
	/// The signed voucher then can be sent to blockchan to be claimed as tokens.
	/// While this happens, the client will use those vouchers to predict the amount of 
	/// </summary>
	public struct Web3WithdrawCommand : IGameCommand
	{
		public ulong Amount;
		public GameId Currency;
		public string Contract;
		public int Chain;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;
		
		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var d = ctx.Data.GetData<Web3PlayerData>();
			if (!ctx.Logic.Web3().ValidWeb3Currencies.Contains(Currency))
			{
				throw new Exception($"{Currency} is not a valid currency");
			}
			
			var currencyLogic = ctx.Logic.CurrencyLogic();
			var amt = currencyLogic.Currencies[Currency];
			if (amt == 0)
			{
				return UniTask.CompletedTask;
			}

			if (Amount != 0)
			{
				amt = Amount;
			}
			
			//ctx.Logic.CurrencyLogic().DeductCurrency(GameId.GasTicket, 1);
			ctx.Logic.CurrencyLogic().DeductCurrency(Currency, amt);
			ctx.Logic.Web3().AwardWeb3Currency(Currency, amt, Contract, Chain);
			return UniTask.CompletedTask;
		}
	}
}