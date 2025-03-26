using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles currency display on the screen. Because of legacy reasons all the logic
	/// is still handled in the CurrencyDisplayElement.
	/// </summary>
	public class CryptoCurrenciesDisplayView : UIView
	{
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		public List<GameId> ShowOnly;

		protected CryptoCurrenciesDisplayElement CryptoCurrenciesElement { get; set; }

		protected override void Attached()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			CryptoCurrenciesElement = (CryptoCurrenciesDisplayElement) Element;
			Setup();
		}
		
		private void Setup()
		{
			SetupCryptoCurrenciesChangeObservable();
		}

		public IWeb3Currency GetWeb3Currency()
		{
			return _gameDataProvider.Web3Data.OnChainData.GetWeb3Currencies()[CryptoCurrenciesElement.MainCurrency];
		}

		private void SetupCryptoCurrenciesChangeObservable()
		{
			var coin = CryptoCurrenciesElement.MainCurrency;
			FLog.Info("Setting up web3 currency view "+coin);
			var data = MainInstaller.ResolveData();
			if (!MainInstaller.ResolveWeb3().IsEnabled())
			{
				data.CurrencyDataProvider.Currencies.Observe(coin, (id, oldV, newV, c) =>
				{
					SetupCryptoCurrenciesView(newV);
					AnimateCurrency(oldV, newV, true); // going to bank
				});
				SetupCryptoCurrenciesView(data.CurrencyDataProvider.Currencies[coin]);
				return;
			}

			var currencies = MainInstaller.ResolveWeb3().GetWeb3Currencies();
			var c = currencies[coin];
			c.TotalPredicted.Observe(ReloadCryptoCurrencies);
			_gameDataProvider.CurrencyDataProvider.Currencies.Observe(coin, (_,old,newv,_) =>
			{
				AnimateCurrency(old, newv, false); // going to bank
				SetupCryptoCurrenciesView(c.TotalPredicted.Value);
			});
			SetupCryptoCurrenciesView(c.TotalPredicted.Value);
			c.UpdateTotalValue();
		}

		private void ReloadCryptoCurrencies(ulong val1, ulong val2)
		{
			var coin = CryptoCurrenciesElement.MainCurrency;
			FLog.Verbose($"Crypto {coin} updating from {val1} to {val2}");
			SetupCryptoCurrenciesView(val2);
			if (val2 > val1)
			{
				AnimateCurrency(val1, val2, true);
			}
		}
		
		private void AnimateCurrency(ulong val1, ulong val2, bool updateText)
		{
			var coin = CryptoCurrenciesElement.MainCurrency;
			CryptoCurrenciesElement.AnimateCurrencyEffect(coin, val1, val2, Presenter.GetCancellationTokenOnClose(), updateText);
		}

		private void SetupCryptoCurrenciesView(ulong totalOnChain)
		{
			var inBank = _gameDataProvider.CurrencyDataProvider.Currencies
				.Where(c =>
					GameIdGroup.CryptoCurrency.GetIds().Contains(c.Key) && ((c.Value > 0 && ShowOnly == null) || (ShowOnly?.Contains(c.Key) ?? false)))
				.ToDictionary(c => c.Key, c => c.Value);

			//TODO We need to discuss how we are going to show Festive coins inside the GAME
			// Currently, Festive Coins are placed within the CryptoCurrency GameIDGroups so they appear in the CryptoMultipleCurrency element.
			// This made sense before the component was updated to fetch cryptocurrency amounts directly from the chain, instead of using player data.
			// if (!MainInstaller.ResolveWeb3().IsEnabled())
			// {
			// 	inBank.Clear();
			// }
			
			CryptoCurrenciesElement.SetData((int)totalOnChain, inBank);
		}

		public override void OnScreenClose()
		{
			if (MainInstaller.ResolveWeb3().IsEnabled())
			{
				_gameDataProvider.Web3Data.OnChainData
					.GetWeb3Currencies()[CryptoCurrenciesElement.MainCurrency].TotalPredicted.StopObservingAll(this);
			}
			else
			{
				_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
			}
			
		}

	}
}