using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	public class JoinWithCodePopupView : UIView
	{
		private readonly Action<string> _onJoin;

		public JoinWithCodePopupView(Action<string> onJoin)
		{
			_onJoin = onJoin;
		}

		protected override void Attached()
		{
			Element.Q<LocalizedButton>("JoinButton").clicked += () =>
			{
				_onJoin.Invoke(Element.Q<LocalizedTextField>("CodeInput").value);
			};
		}
	}
}