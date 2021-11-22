using System;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	/// <remarks>
	/// It adds extra information for Hard currency spending
	/// </remarks>
	public class GenericDialogHcPresenter : GenericDialogPresenterBase
	{
		[SerializeField] private Image _hcImage;
		[SerializeField] private Image _scImage;
		[SerializeField] private TextMeshProUGUI _hardCurrencyCostText;

		/// <inheritdoc cref="GenericDialogService.OpenHcDialog"/>
		public  void SetInfo(string title, string cost, bool showCloseButton, 
		                          GenericDialogButton button, bool showSC = false, Action closeCallback = null)
		{
			_hcImage.enabled = !showSC;
			_scImage.enabled = showSC;
			_hardCurrencyCostText.text = cost;
			SetBaseInfo(title, showCloseButton, button, closeCallback);
		}
	}
}

