using System;
using FirstLight.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class GenericChoiceDialogPresenter : GenericDialogPresenterBase
	{
		[SerializeField] protected Button Choice2Button;
		[SerializeField] protected TextMeshProUGUI Choice2ButtonText;
		
		/// <summary>
		/// Shows the Generic Dialog PopUp with the given <paramref name="title"/> and the <paramref name="button1"/> information.
		/// and the <paramref name="button2"/> information.
		/// </summary>
		public void SetInfo(string title, GenericDialogButton button1, GenericDialogButton button2)
		{
			SetBaseInfo(title, false, button1, null);
			
			Choice2Button.gameObject.SetActive(true);
			Choice2ButtonText.text = button2.ButtonText;
			Choice2Button.onClick.RemoveAllListeners();
			Choice2Button.onClick.AddListener(Close);
			Choice2Button.onClick.AddListener(button2.ButtonOnClick);
		}
	}
}

