using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class CurrencyDisplayView : UIView
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