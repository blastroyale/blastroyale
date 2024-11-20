using System.Collections.Generic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Responsible for handling all currencies in the top bar, initializing and exposing methods to manage them
	/// </summary>
	public class CurrencyTopBarView : UIView
	{
		public static GameId[] DefaultValues = {GameId.COIN, GameId.BlastBuck};
		private static string[] DefaultClasses = new[] {"anim-translate", "anim-translate--down-s", "currency-display-space"};

		protected override void Attached()
		{
		}

		public void Configure(VisualElement animationOrigin = null, List<GameId> showOnly = null)
		{
			Element.Clear();
			var count = 0;

			foreach (var defaultValue in DefaultValues)
			{
				if (showOnly != null && !showOnly.Contains(defaultValue)) continue;
				var element = new CurrencyDisplayElement();
				element.SetCurrency(defaultValue);
				element.AddClass(DefaultClasses);
				element.AddClass("anim-delay-" + count);
				Element.Add(element);
				element.SetData(animationOrigin, false, Presenter.GetCancellationTokenOnClose());
				var view = new CurrencyDisplayView();
				Presenter.AddView(element, view);
				count++;
			}

			var cryptoElement = new CryptoCurrenciesDisplayElement();
			cryptoElement.SetMainCurrency(GameId.NOOB);
			var cryptoView = new CryptoCurrenciesDisplayView()
			{
				ShowOnly = showOnly
			};
			cryptoElement.SetAnimationOrigin(animationOrigin);
			cryptoElement.AddClass(DefaultClasses);
			cryptoElement.AddClass("anim-delay-" + count);
			Element.Add(cryptoElement);
			Presenter.AddView(cryptoElement, cryptoView);
			cryptoView.Setup();
		}
	}
}