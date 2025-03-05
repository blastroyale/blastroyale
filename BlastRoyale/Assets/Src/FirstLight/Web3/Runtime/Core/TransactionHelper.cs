using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services.Analytics.Events;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Nethereum.GnosisSafe.ContractDefinition;
using Newtonsoft.Json;
using Sequence.Contracts;
using Sequence.EmbeddedWallet;

namespace FirstLight.Web3.Runtime
{
	public enum TransactionStatus
	{
		Sending, Mining, Confirmed, Validated, Failed
	}
	
	public class TransactionData
	{
		public TransactionStatus Status;
		public string TransactionId;
	}

	public class TransactionResult
	{
		public SuccessfulTransactionReturn Result;
		public FailedTransactionReturn Error;

		public bool IsSuccess() => Error == null;
		public bool HasBeenMined() => Result?.receipt?.status == "1";
	}
	
	public class TransactionWrapper
	{
		public Action<TransactionStatus, TransactionStatus> OnStateUpdate;
		private IWeb3ExternalService _service;
		private TransactionData _currentState = new ();
		private TransactionResult _result = new ();
		private List<Transaction> _currentBatch = new ();
		private static List<TransactionWrapper> _batches = new ();
		
		private bool _waitMine;
		
		public bool IsSuccess() => _result.IsSuccess();
		
		public string TransactionId => _result?.Result?.txHash;

		public string ErrorMessage => _result?.Error?.error;
		
		public TransactionWrapper(IWeb3ExternalService service)
		{
			_service = service;
		}

		private void UpdateState(TransactionStatus state)
		{
			var prev = _currentState.Status;
			_currentState.Status = state;
			OnStateUpdate?.Invoke(prev, state);
		}

		public void EnqueueCall(CallContractFunction function)
		{
			_currentBatch.Add(new RawTransaction(function));
		}

		private async UniTask SendTransactionInternal()
		{
			_currentState = new ();
			UpdateState(TransactionStatus.Sending);
			var chain = _service.Indexer.GetChain();
			var tx = await _service.Wallet.SendTransaction(chain, _currentBatch.ToArray()).AsUniTask();
			if (tx is SuccessfulTransactionReturn r)
			{
				FLog.Verbose("Transaction sent "+JsonConvert.SerializeObject(r, Formatting.Indented));
				_currentState.TransactionId = r.txHash;
				_result.Result = r;
				Web3Analytics.SendEvent("client_tx_sent", ("tx", r.txHash));
				UpdateState(TransactionStatus.Mining);
				if (_waitMine)
				{
					await WaitToBeMined();
				}
			} else if (tx is FailedTransactionReturn e)
			{
				_result.Error = e;
				UpdateState(TransactionStatus.Failed);
				Web3Analytics.SendEvent("client_tx_failed", ("error", e.error), ("id", e.request.data["identifier"]));
				FLog.Warn(JsonConvert.SerializeObject(e, Formatting.Indented));
			}
		}
		
		public async UniTask<TransactionWrapper> SendTransactionBatch(bool waitMine = false)
		{
			_waitMine = waitMine;
			await UniTask.WaitUntil(() => _batches.Count == 0); // until prediction only 1 batch at a time
			_batches.Add(this);
			await SendTransactionInternal();
			_batches.Remove(this);
			return this;
		}
		
		public TransactionStatus GetStatus() {
			if(_result == null) return TransactionStatus.Sending;
			if (!_result.IsSuccess()) return TransactionStatus.Failed;
			if(!_result.HasBeenMined()) return TransactionStatus.Mining;
			return TransactionStatus.Confirmed;
		}
		
		public bool HasBeenSent() => _result != null && _result.IsSuccess();

		public async UniTask WaitToBeMined()
		{
			if (_result != null)
			{
				await _service.Wallet.WaitForTransactionReceipt(_result.Result).AsUniTask();
				UpdateState(TransactionStatus.Confirmed);
			}
			else
			{
				FLog.Error("Waiting tx not sent to be mined");
			}
		}
	}
}