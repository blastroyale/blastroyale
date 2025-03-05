using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Web3.Runtime.SequenceContracts;

namespace FirstLight.Web3.Runtime
{
	[Serializable]
	public class VoucherState
	{
		public string VoucherId;
		public string TransactionId;
	}
	
	[Serializable]
	public class VoucherUserData
	{
		public List<VoucherState> States = new ();
	}
	
	public class VoucherService
	{
		public List<Web3Voucher> Sending = new ();
		private IWeb3ExternalService _web3Game;
		public bool Run = true;
		private Queue<Web3Voucher> _completed = new ();
		private AsyncBufferedQueue _queue = new (TimeSpan.FromSeconds(0.2), false);
		private VoucherUserData _userData;
		
		public VoucherService(IWeb3ExternalService web3Game)
		{
			_web3Game = web3Game;
			_web3Game.GameServices.MessageBrokerService.Subscribe<VoucherCreatedMessage>(OnVoucherCreated);
			_queue.Add(Tick);	
		}

		private void OnVoucherCreated(VoucherCreatedMessage m)
		{
			FLog.Verbose("Voucher created, checking for new vouchers signed from server");
			WaitForBackendSignature(m.Voucher.VoucherId).ContinueWith(_ =>
			{
				_queue.Add(Tick);
			}).Forget();
		}

		// Cries for not having socket implemented
		private async UniTask<Web3PlayerData> WaitForBackendSignature(Guid voucherId)
		{
			var clientData = _web3Game.GameServices.DataService.GetData<Web3PlayerData>();
			var serverData = await PlayfabUserExtensions.ReadFromUserReadonlyData<Web3PlayerData>();
			var tries = 5;
			while (tries > 0 && clientData.Vouchers.Count != serverData.Vouchers.Count)
			{
				await UniTask.WaitForSeconds(0.2f);
				serverData = await PlayfabUserExtensions.ReadFromUserReadonlyData<Web3PlayerData>();
				tries--;
				FLog.Verbose("Try waiting server update "+tries);
			}
			_web3Game.GameServices.DataService.AddData(serverData);
			return clientData;
		}
		
		// Client tick operation. Has to be atomic and no one else can modify VoucherUserData
		public async UniTask Tick()
		{
			FLog.Info("Checking pending vouchers");
	
			// Sync data with client
			var serverVouchers = _web3Game.GameServices.DataService.GetData<Web3PlayerData>();//await PlayfabUserExtensions.ReadFromUserReadonlyData<Web3PlayerData>();
			if (_userData == null)
			{
				_userData = await PlayfabUserExtensions.ReadFromUserData<VoucherUserData>() ?? new VoucherUserData();
			}
			if (serverVouchers?.Vouchers == null) return;
			
			_web3Game.GameServices.DataService.AddData(serverVouchers);

			foreach (var voucher in serverVouchers.Vouchers.ToArray())
			{
				await CheckIfNeedCreateTransaction(_userData, voucher);
			}

			foreach (var tx in _userData.States.ToArray())
			{
				if (serverVouchers.Vouchers.All(v => v.VoucherId.ToString() != tx.VoucherId))
				{
					_userData.States.Remove(tx);
					await PlayfabUserExtensions.SaveInUserData(_userData); 
					Web3Analytics.SendEvent("client_dangle_tx", ("voucher", tx.VoucherId), ("tx", tx.TransactionId));
					FLog.Info("Removing state of transaction that client is not aware of");
				}
			}
			
			FLog.Info("Finished checking pending vouchers");
		}

		private async UniTask<bool> HasMinedAndSuccess(VoucherState state)
		{
			var tx = await _web3Game.Rpc.TransactionReceipt(state.TransactionId);
			FLog.Verbose($"Checked tx {tx.txnHash} status: {tx.status}");
			return tx.status == "1";
		}

		private async UniTask CheckIfNeedCreateTransaction(VoucherUserData userData, Web3Voucher voucher)
		{
			var gameId = VoucherTypes.ByVoucherType[voucher.Type];
			var web3CurrencyConfig = _web3Game.Web3Config.FindCurrency(gameId);
			if (web3CurrencyConfig == null)
			{
				throw new Exception($"Could not find web3 currency config for currency {gameId}");
			}
			(string, object) [] voucherAnalyticsData =  {("voucher", voucher.ToString()), ("userData", userData) };
			var currency = _web3Game.GetCurrency(gameId);
			var idString = voucher.VoucherId.ToString();
			var existing = userData.States.FirstOrDefault(s => s.VoucherId == idString);
			if (existing != null)
			{
				if (await HasMinedAndSuccess(existing))
				{
					FLog.Verbose("Cleaning up already finished voucher");
					userData.States.Remove(existing);
					await PlayfabUserExtensions.SaveInUserData(userData);
					_web3Game.Web3PlayerData.SendConsumeVoucherCommand(voucher);
					currency.UpdateTotalValue();
					Web3Analytics.SendEvent("client_voucher_duplicated", voucherAnalyticsData);
					return;
				}
				userData.States.Remove(existing);
				FLog.Warn($"Transaction already failed for voucher {voucher.VoucherId} tx {existing.TransactionId}, retrying");
			}
			//_web3Game.GameServices.InGameNotificationService.QueueNotification("[TST] 1 Claim Transaction Sent");
			var tx = new TransactionWrapper(_web3Game);
			currency.VoucherContract.PrepareRedeemTransaction(tx, voucher);
			await tx.SendTransactionBatch();
			if (tx.HasBeenSent())
			{
				FLog.Verbose($"Claim transaction sent {tx.TransactionId}");
				
				var state = new VoucherState() { VoucherId = voucher.VoucherId.ToString(), TransactionId = tx.TransactionId };
				userData.States.Add(state);
				await PlayfabUserExtensions.SaveInUserData(userData);

				FLog.Verbose($"Waiting for blockchain to mine the transaction {tx}");
				await tx.WaitToBeMined();
				
				_web3Game.Web3PlayerData.SendConsumeVoucherCommand(voucher);
				Web3Analytics.SendEvent("client_voucher_consumed", voucherAnalyticsData);
				userData.States.Remove(state);
				await PlayfabUserExtensions.SaveInUserData(userData);
				
				//_web3Game.GameServices.InGameNotificationService.QueueNotification("[TST] 1 Claim Transaction Finished");
				FLog.Info($"Transaction {tx.TransactionId} Mined & voucher {voucher.VoucherId} consumed");
			}
			else
			{
				if (tx.ErrorMessage == VoucherContract.ERR_ALREADY_USED)
				{
					FLog.Info($"Consuming already used voucher {voucher.VoucherId}");
					_web3Game.Web3PlayerData.SendConsumeVoucherCommand(voucher);
					Web3Analytics.SendEvent("client_voucher_already_used", voucherAnalyticsData);
					currency.UpdateTotalValue();
				}
			}
		}
	}
}