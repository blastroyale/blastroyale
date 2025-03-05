using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Web3.Runtime.SequenceContracts;
using Sequence;
using UnityEngine;

namespace FirstLight.Web3.Runtime
{
	public class Web3Currency : IWeb3Currency
	{
		public ShopContract ShopContract { get; }
		public VoucherContract VoucherContract { get; }
		public CurrencyContract CurrencyContract { get; }
		public IObservableField<ulong> TotalPredicted { get; }
		public IObservableField<ulong> OnChainValue { get; }
		public IObservableField<ulong> OffChainValue { get; }
		
		private IGameDataProvider _data;
		private IWeb3ExternalService _webExt;
		private CurrencyConfig _config;
		private AsyncBufferedQueue _queue = new (TimeSpan.FromSeconds(1), true);
		
		public Web3Currency(IWeb3ExternalService service, CurrencyConfig config)
		{
			var services = MainInstaller.ResolveServices();
			var msgb = services.MessageBrokerService; 
			_data = MainInstaller.ResolveData();
			_webExt = service;
			_config = config;
			VoucherContract = new VoucherContract(service, config.ClaimContract);
			CurrencyContract = new CurrencyContract(service, config.Contract);
			ShopContract = new ShopContract(service, config.GameId, config.ShopContract);
			TotalPredicted = new ObservableField<ulong>(0);
			OnChainValue = new ObservableField<ulong>(0);
			OffChainValue = new ObservableField<ulong>(0);
			msgb.Subscribe<VoucherCreatedMessage>(e => OnVoucherAdded(e.Voucher));
			msgb.Subscribe<VoucherConsumedMessage>(e => OnVoucherRemoved(e.Voucher));
			_webExt.OnWeb3Ready += ListenForBlockchainUpdates;
			
			var prev = PreviousSavedValue;
			FLog.Verbose($"Cached value blockckchain currency for {_config.GameId}: {prev}");
			TotalPredicted.Value = prev;
		}

	
		private void ListenForBlockchainUpdates()
		{
			var eventFilter = new EventFilter
			{
				accounts = new [] { _webExt.Wallet.GetWalletAddress().Value },
				contractAddresses = new[] { _config.Contract },
				events = new[] {"Transfer(address indexed from, address indexed to, uint256 value)"}
			};

			_webExt.Indexer.SubscribeEvents(new SubscribeEventsArgs(eventFilter), new WebRPCStreamOptions<SubscribeEventsReturn>(
				OnSubscribeEventsMessageReceived,
				OnWebRPCErrorReceived));
			FLog.Info($"Listening for blockchain updates for currency {_config.GameId} ");
		}
		
		private void OnSubscribeEventsMessageReceived(SubscribeEventsReturn e)
		{
			UpdateTotalValue();
		}
		
		private void OnWebRPCErrorReceived(WebRPCError error)
		{
			FLog.Error($"OnWebRPCErrorReceived: {error.msg}");
		}

		public ulong PreviousSavedValue => (ulong) PlayerPrefs.GetInt($"Last{_config.GameId}", 0);
		public void SavePreviousValue(int value) => PlayerPrefs.SetInt($"Last{_config.GameId}", value);

		private void OnVoucherRemoved(Web3Voucher voucher)
		{
			if (_config.GameId == VoucherTypes.ByVoucherType[voucher.Type])
			{
				//UpdateTotalValue();
			}
		}
		
		private void OnVoucherAdded(Web3Voucher voucher)
		{
			if (_config.GameId == VoucherTypes.ByVoucherType[voucher.Type])
			{
				UpdateTotalValue();
			}
		}

		private async UniTask<BigInteger> UpdateBlockchainValue()
		{
			FLog.Verbose($"Reading web3 currency {_config.GameId} from blockchain");

			var status = await _webExt.Indexer.RuntimeStatus();
			while (!status.indexerEnabled || !status.healthOK)
			{
				status = await _webExt.Indexer.RuntimeStatus();
			}
			await UniTask.WaitUntil(() => _webExt.Wallet != null);
			var args = new GetTokenBalancesArgs(_webExt.Wallet.GetWalletAddress(), _config.Contract);
			var r = await _webExt.Indexer.GetTokenBalances(args);
			var balance = r.balances?.FirstOrDefault()?.balance ?? BigInteger.Zero;
			return (BigInteger)Nethereum.Web3.Web3.Convert.FromWei(balance);
		}
		
		public BigInteger UpdateOffchainPrediction()
		{
			OffChainValue.Value = (ulong)_data.Web3Data.GetCurrencyAsVouchers(_config.GameId);
			TotalPredicted.Value = OffChainValue.Value + OnChainValue.Value;
			return TotalPredicted.Value;
		}

		public void UpdateTotalValue()
		{
			_queue.Add(Update);
		}

		private async UniTask Update()
		{
			FLog.Info($"Updating web3 currency {_config.GameId} predicted value");
			OnChainValue.Value = (ulong) await UpdateBlockchainValue();
			OffChainValue.Value = (ulong)_data.Web3Data.GetCurrencyAsVouchers(_config.GameId);
			TotalPredicted.Value = OffChainValue.Value + OnChainValue.Value;
			SavePreviousValue((int)TotalPredicted.Value);
			FLog.Verbose($"Calculated {_config.GameId} value as {OnChainValue.Value} OnChain, {OffChainValue.Value} OffChain and {TotalPredicted.Value} Total");

		}
	}
}