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
	public class CurrencyDisplayView : UIView2
	{
		private CurrencyDisplayElement _currency;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_currency = (CurrencyDisplayElement) element;

			_currency.Init(MainInstaller.Resolve<IGameDataProvider>(),
				MainInstaller.Resolve<IMainMenuServices>(),
				MainInstaller.Resolve<IGameServices>());
		}

		public override void SubscribeToEvents()
		{
			_currency.SubscribeToEvents();
		}

		public override void UnsubscribeFromEvents()
		{
			_currency.UnsubscribeFromEvents();
		}
	}
}