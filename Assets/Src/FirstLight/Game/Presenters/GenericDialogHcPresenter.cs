using System;
using FirstLight.Game.Services;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	/// <remarks>
	/// It adds extra information for Hard currency spending
	/// </remarks>
	public class GenericDialogHcPresenter : GenericDialogPresenterBase
	{
		[SerializeField, Required] private Image _hcImage;
		[SerializeField, Required] private Image _scImage;
		[SerializeField, Required] private TextMeshProUGUI _hardCurrencyCostText;

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

