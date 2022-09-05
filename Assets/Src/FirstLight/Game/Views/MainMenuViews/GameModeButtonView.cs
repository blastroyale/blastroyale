using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
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
		[SerializeField, Required] private Transform _tooltipAnchor;
		[SerializeField, Required] private TextMeshProUGUI _gameModeText;
		[SerializeField, Required] private TextMeshProUGUI _matchTypeText;

		private IGameServices _services;

		private MatchType _matchType;
		private string _gameModeId;
		private List<string> _mutators;
		private bool _fromRotation;
		private DateTime _endTime;
		private Action<string, List<string>, MatchType, bool, DateTime> _onClick;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_tooltipButton.onClick.AddListener(OnTooltipButtonClick);
			_selectButton.onClick.AddListener(OnButtonClick);
		}

		public void Init(string gameModeId, List<string> mutators, MatchType matchType, bool fromRotation, DateTime endTime,
		                 Action<string, List<string>, MatchType, bool, DateTime> onClick)
		{
			_gameModeId = gameModeId;
			_mutators = mutators;
			_matchType = matchType;
			_fromRotation = fromRotation;
			_endTime = endTime;
			_onClick = onClick;

			// TODO: Display mutator icons
			_gameModeText.text = _gameModeId.ToUpper();
			_matchTypeText.text = _matchType.GetTranslation();
		}

		private void OnDestroy()
		{
			_tooltipButton.onClick.RemoveAllListeners();
		}

		private void OnButtonClick()
		{
			_onClick(_gameModeId, _mutators, _matchType, _fromRotation, _endTime);
		}

		private void OnTooltipButtonClick()
		{
			var tooltip = _matchType switch
			{
				MatchType.Casual => ScriptLocalization.Tooltips.ToolTip_Casual,
				MatchType.Ranked => ScriptLocalization.Tooltips.ToolTip_Ranked,
				_ => throw new ArgumentOutOfRangeException()
			};

			_services.GenericDialogService.OpenTooltipDialog(tooltip, _tooltipAnchor.position,
			                                                 TooltipArrowPosition.Top);
		}
	}
}