using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class is responsible for presenting player with various info of a given game mode
	/// Currently customised on-prefab, but in future iteration, would be initialized dynamically with a list
	/// of currently available game modes.
	/// </summary>
	public class GameModeButtonView : MonoBehaviour
	{
		[SerializeField, Required] private Button _tooltipButton;
		[SerializeField, Required] private MatchType _matchType;
		[SerializeField, Required] private Transform _tooltipAnchor;
		
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_tooltipButton.onClick.AddListener(OnTooltipButtonClick);
		}

		private void OnDestroy()
		{
			_tooltipButton.onClick.RemoveAllListeners();
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