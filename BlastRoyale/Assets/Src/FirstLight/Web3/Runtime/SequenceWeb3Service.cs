using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Web3.Runtime.SequenceContracts;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using Quantum.Allocator;
using Sequence;
using Sequence.Config;
using Sequence.EmbeddedWallet;
using Sequence.Provider;
using UnityEngine;
using Chain = Sequence.Chain;


namespace FirstLight.Web3.Runtime
{
	/// <summary>
	/// Integrates IMX Passport to FLG Game
	/// </summary>
	public class SequenceWeb3GameWeb3Service : MonoBehaviour, IWeb3ExternalService, IAuthenticationHook
	{
		private Web3State _state;
		public event Action OnWeb3Ready;
		private Dictionary<GameId, IWeb3Currency> _currencies { get; set; }
		public IGameServices GameServices { get;  private set;}
		public IIndexer Indexer { get;  private set;}
		public SequenceEthClient Rpc { get; set; }
		public Web3Config Web3Config { get;  private set;}
		public Web3PlayerDataService Web3PlayerData { get; private set; }
		public SequenceWallet Wallet { get; private set; }
		public VoucherService VoucherService { get; private set; }
		public Web3ShopService web3ShopService { get; private set; }
		public IGameDataProvider GameData => MainInstaller.ResolveData();
		public Web3Currency GetCurrency(GameId id) => (Web3Currency)_currencies[id];

		public bool IsEnabled()
		{
			return MainInstaller.ResolveData().Web3Data.CanUseWeb3() && Web3Config != null;
		}
	
		public event Action<Web3State> OnStateChanged;
	
		private void Awake()
		{
			DontDestroyOnLoad(this.gameObject);
			StartAsync().Forget();
		}

		private async UniTaskVoid StartAsync()
		{
			GameServices = await MainInstaller.WaitResolve<IGameServices>();
			MainInstaller.Bind<IWeb3Service>(this);
			GameServices.AuthService.RegisterHook(this);
			((IAPService)GameServices.IAPService).PrePurchaseHooks.Add(OnPreparePurchase);
			State = Web3State.Available;
		}

		public async UniTask Transfer(GameId currency, string to, BigInteger amount)
		{
			FLog.Info($"Transfering {amount} {currency} to {to}");
			var addr = new Address(to);
			var tx = new TransactionWrapper(this);
			GetCurrency(currency).CurrencyContract.PrepareTransferTransaction(tx, to, amount);
			await tx.SendTransactionBatch();
			if (!tx.IsSuccess())
			{
				throw new Exception("Error transfering " + tx.ErrorMessage);
			}
			//await tx.WaitToBeMined();
		}

		public void Withdrawal(GameId currency)
		{
			var config = GetCurrency(currency);
			GameServices.CommandService.ExecuteCommand(new Web3WithdrawCommand()
			{
				Contract = config.VoucherContract.Contract.GetAddress(),
				Chain = (int)Indexer.GetChain(),
				Currency = GameId.NOOB
			});
		}
		
		private async UniTask OnPreparePurchase(GameProduct product)
		{
			try
			{
				if (!IsEnabled()) return;
				if (!product.IsWeb3Purchase()) return;
				await GameServices.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
				var price = product.GetPrice();
				var currency = price.item;
				var cost = price.amt;
				FLog.Info($"Buyng web3 item: {product.GameItem.Id} for {cost}x {currency}");
				var cur = GetCurrency(currency);
				cur.TotalPredicted.Value -= price.amt;
				var tx = new TransactionWrapper(this);
				cur.ShopContract.PrepareBuyTransaction(tx, product.GameItem, cost);
				GameServices.InGameNotificationService.QueueNotification("Half-baked prediction flow...");
				var r = await tx.SendTransactionBatch();
				if (!r.IsSuccess())
				{
					GameServices.InGameNotificationService.QueueNotification("[Dbg] Some shit happened " + r.ErrorMessage);
					return;
				}
				await GameServices.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
			}
			catch (Exception e)
			{
				FLog.Error("PRE PURCHASE CAGOU "+e.Message);
				throw e;
			}
		}

		private void OpenTransfer()
		{
			GameServices.GenericDialogService.OpenInputDialog("Transfer", "Send 10 noob to other wallets", "", new GenericDialogButton<string>()
			{
				ButtonText = "Transfer 10 Noob"
			}, true, address =>
			{
				var tx = new TransactionWrapper(this);
				GetCurrency(GameId.NOOB).CurrencyContract.PrepareTransferTransaction(tx, address, 10);
				tx.SendTransactionBatch(true).ContinueWith(t =>
				{
					if (!t.HasBeenSent())
					{
						GameServices.InGameNotificationService.QueueNotification("[Dbg] Err "+t.ErrorMessage);
					}
				}).Forget();
			});
		}
		
		private UniTask Init(LoginResult res)
		{
			if (!GameServices.GameAppService.AppData.TryGetValue("ChainConfig", out var chainConfigJson))
			{
				Debug.LogError("No chain config json in playfab. Web3 not initializing");
				return UniTask.CompletedTask;
			}

			if (chainConfigJson == null)
			{
				FLog.Warn("Web3 Disabled");
				return UniTask.CompletedTask;
			}
			Web3Config = ModelSerializer.Deserialize<Web3Config>(chainConfigJson);
			if (!IsEnabled())
			{
				FLog.Warn("Web3 Disabled");
				return UniTask.CompletedTask;
			}
			FLog.Verbose("Web3Config: "+chainConfigJson);
			_currencies = new ();
			foreach (var currency in Web3Config.Currencies)
			{
				_currencies[currency.GameId] = new Web3Currency(this, currency);
				FLog.Verbose("Web3 currency registered " + currency.GameId);
			}
			
			if (FLEnvironment.Current == FLEnvironment.PRODUCTION)
			{
				FLog.Info("Using production web3 sequence");
				var devConfig = SequenceConfig.GetConfig();
				var prodConfig = Resources.Load<SequenceConfig>("SequenceConfigProduction");
				devConfig.WaaSConfigKey = prodConfig.WaaSConfigKey;
				devConfig.BuilderAPIKey = prodConfig.BuilderAPIKey;
				Indexer = new ChainIndexer(Chain.Base);
			}
			else
			{
				Indexer = new ChainIndexer(Chain.TestnetBaseSepolia);
			}
			Indexer.Ping().AsUniTask().ContinueWith(r =>
			{
				if (!r) throw new Exception("Could not connect to Sequence web3 indexer");
			}).Forget();

			SequenceLogin login = SequenceLogin.GetInstance();
			SequenceWallet.OnWalletCreated += OnWalletAuth;
			login.PlayFabLogin(PlayFabSettings.staticSettings.TitleId, res.SessionTicket, "");
			return UniTask.CompletedTask;
		}

		private void OnWalletAuth(SequenceWallet wallet)
		{
			Wallet = wallet;
			FLog.Verbose("Embedded wallet created " + wallet.GetWalletAddress());
			Web3PlayerData = new Web3PlayerDataService(wallet);
			VoucherService = new VoucherService(this);
			web3ShopService = new Web3ShopService(this);
			Rpc = new SequenceEthClient(Indexer.GetChain());
			FLog.Info("Wallet authenticated "+Wallet.GetWalletAddress().Value);
			State = Web3State.Authenticated;
			web3ShopService.CheckIfOverpaid().Forget();
			OnWeb3Ready?.Invoke();
		}
	
		public IReadOnlyDictionary<GameId, IWeb3Currency> GetWeb3Currencies()
		{
			return _currencies;
		}
	
		public Web3State State
		{
			get => _state;
			private set
			{
				if (value != _state)
				{
					OnStateChanged?.Invoke(value);
				}
				_state = value;
			}
		}

		public Web3State GetState()
		{
			return _state;
		}

		public string CurrentWallet => Wallet.GetWalletAddress();
		
		public UniTask BeforeAuthentication(bool previouslyLoggedIn)
		{
			return UniTask.FromResult(false);
		}

		public  UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn)
		{
			return UniTask.FromResult(false);
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return Init(result);
		}
		
		public async UniTask BeforeLogout()
		{
			if (!IsEnabled() || Wallet == null) return;
			await Wallet.DropThisSession();
		}
	}
}