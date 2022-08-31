using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class is responsible for presenting player with various info of a given game mode
	/// Currently customised on-prefab, but in future iteration, would be initialized dynamically with a list
	/// of currently available game modes.
	/// </summary>
	public class GameModeButtonView : MonoBehaviour
	{
		[SerializeField, Required] private Button _selectButton;
		[SerializeField, Required] private Button _tooltipButton;
		[SerializeField, Required] private MatchType _matchType;
		[SerializeField, Required] private GameMode _gameMode;
		[SerializeField, Required] private Transform _tooltipAnchor;
		[SerializeField, Required] private TextMeshProUGUI _gameModeText;
		[SerializeField, Required] private TextMeshProUGUI _matchTypeText;
		
		/// <summary>
		/// Action that invokes when the button is clicked, with the selected game mode and match type
		/// </summary>
		public Action<GameMode, MatchType> GameModeAndMatchTypeSelected { get; set; }
		
		private IGameServices _services;
		
		private void Awake()
		{
			if (_gameMode == GameMode.Deathmatch && !FeatureFlags.DEATHMATCH_ENABLED)
			{
				gameObject.SetActive(false);
				return;
			}
			
			_services = MainInstaller.Resolve<IGameServices>();
			_tooltipButton.onClick.AddListener(OnTooltipButtonClick);
			_selectButton.onClick.AddListener(OnButtonClick);

			_gameModeText.text = _gameMode.GetTranslation();
			_matchTypeText.text = _matchType.GetTranslation();
		}

		private void OnDestroy()
		{
			_tooltipButton.onClick.RemoveAllListeners();
		}

		private void OnButtonClick()
		{
			GameModeAndMatchTypeSelected?.Invoke(_gameMode,_matchType);
		}

		private void OnTooltipButtonClick()
		{
			string tooltip = "";

			switch (_matchType)
			{
				case MatchType.Casual:
					tooltip = ScriptLocalization.Tooltips.ToolTip_Casual;
					break;
				
				case MatchType.Ranked:
					tooltip = ScriptLocalization.Tooltips.ToolTip_Ranked;
					break;
			}
			
			_services.GenericDialogService.OpenTooltipDialog(tooltip, _tooltipAnchor.position, TooltipArrowPosition.Top);
		}
	}
}