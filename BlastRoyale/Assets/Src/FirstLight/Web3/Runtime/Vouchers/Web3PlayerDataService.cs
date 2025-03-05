using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sequence.EmbeddedWallet;

namespace FirstLight.Web3.Runtime.SequenceContracts
{
	/// <summary>
	/// Connects web3 world with player data
	/// </summary>
	public class Web3PlayerDataService
	{
		private readonly IGameServices _services;
		private readonly SequenceWallet _wallet;
		private Game.Data.Web3PlayerData _data;
		
		public Web3PlayerDataService(SequenceWallet wallet)
		{
			_wallet = wallet;
			_services = MainInstaller.ResolveServices();
			_data = _services.DataService.GetData<Game.Data.Web3PlayerData>();
			if(wallet.GetWalletAddress() != _data.Wallet)
			{
				FLog.Info("Updating user wallet in playfab");
				SetupWallet();
			}
		}
		
		private void SetupWallet()
		{
			_services.CommandService.ExecuteCommand(new UpdateWeb3DataCommand()
			{
				PlayerWallet = _wallet.GetWalletAddress()
			});
		}

		/// <summary>
		/// Flags a voucher is consumed on-chain
		/// by writing to player data a removal of the
		/// voucher from the list
		/// </summary>
		public void SendConsumeVoucherCommand(Web3Voucher voucher)
		{
			FLog.Info("Consuming voucher "+voucher);
			_services.CommandService.ExecuteCommand(new ConsumeWeb3VoucherCommand()
			{
				VoucherId = voucher.VoucherId
			});
		}
	}
}