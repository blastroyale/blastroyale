using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View populates the EquipmentDialog with special move stats for a piece of equipment in the front end.
	/// </summary>
	public class EquipmentStatSpecialInfoView : MonoBehaviour
	{
		[SerializeField, Required] private Image _specialImage;
		[SerializeField, Required] private TextMeshProUGUI _specialText;
		[SerializeField, Required] private TextMeshProUGUI _headingText;
		[SerializeField, Required] private Button _specialButton;
		
		private IGameServices _services;
		private GameId _specialGameId;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_specialButton.onClick.AddListener(ShowInfoPopup);
		}

		/// <summary>
		/// Set the information of this specific item, if it's a special move.
		/// </summary>
		public void SetInfo(string title, GameId special, Sprite sprite)
		{
			_specialGameId = special;
			_headingText.text = title;
			_specialText.text = special.GetTranslation();
			_specialImage.sprite = sprite;
		}

		private void ShowInfoPopup()
		{
			var descriptionTerm = _specialGameId.GetTranslationTerm() + GameConstants.DESCRIPTION_POSTFIX;
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.AWESOME,
				ButtonOnClick = CloseDialog
			};

			_services.GenericDialogService.OpenVideoDialog(_specialGameId.GetTranslation(),
				LocalizationManager.GetTranslation(descriptionTerm),
				_specialGameId,
				true, 
				confirmButton);
		}
		
		private void CloseDialog()
		{
			_services.GenericDialogService.CloseDialog();
		}
	}
}