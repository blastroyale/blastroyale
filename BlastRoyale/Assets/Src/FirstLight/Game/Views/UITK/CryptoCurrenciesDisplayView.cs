using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Logic;
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
		}

		public override void OnScreenOpen(bool reload)
		{
			SetupCryptoCurrenciesChangeObservable();
			SetupCryptoCurrenciesView();
		}

		private void SetupCryptoCurrenciesChangeObservable()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.Observe(ReloadCryptoCurrencies);
		}

		private void ReloadCryptoCurrencies(GameId cryptoCurrencyID, ulong val1, ulong val2, ObservableUpdateType observableUpdateType)
		{
			SetupCryptoCurrenciesView();

			if (cryptoCurrencyID == GameId.NOOB && val2 > val1)
			{
				CryptoCurrenciesElement.AnimateCurrencyEffect(GameId.NOOB, val1, val2, Presenter.GetCancellationTokenOnClose());
			}
		}

		public override void OnScreenClose()
		{
			_gameDataProvider.CurrencyDataProvider.Currencies.StopObservingAll(this);
		}

		private void SetupCryptoCurrenciesView()
		{
			var cryptoCurrencies = _gameDataProvider.CurrencyDataProvider.Currencies
				.Where(c =>
					GameIdGroup.CryptoCurrency.GetIds().Contains(c.Key) && c.Value > 0 && (ShowOnly == null || ShowOnly.Contains(c.Key)))
				.ToDictionary(c => c.Key, c => c.Value);

			CryptoCurrenciesElement.SetData(cryptoCurrencies);
		}
	}
}