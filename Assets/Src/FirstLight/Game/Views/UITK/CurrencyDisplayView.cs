using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles currency display on the screen. Because of legacy reasons all the logic
	/// is still handled in the CurrencyDisplayElement.
	/// </summary>
	public class CurrencyDisplayView : UIView
	{
		private CurrencyDisplayElement _currency;

		protected override void Attached()
		{
			_currency = (CurrencyDisplayElement) Element;

			_currency.Init(MainInstaller.Resolve<IGameDataProvider>(),
				MainInstaller.Resolve<IGameServices>());
		}

		public override void OnScreenOpen(bool reload)
		{
			_currency.SubscribeToEvents();
		}

		public override void OnScreenClose()
		{
			_currency.UnsubscribeFromEvents();
		}
	}
}