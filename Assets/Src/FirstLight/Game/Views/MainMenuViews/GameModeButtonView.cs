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
		[SerializeField, Required] private TextMeshProUGUI _timeLeftText;

		private IGameServices _services;

		private GameModeInfo _info;
		private Action<GameModeInfo> _onClick;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_tooltipButton.onClick.AddListener(OnTooltipButtonClick);
			_selectButton.onClick.AddListener(OnButtonClick);
		}

		public void Init(GameModeInfo info, Action<GameModeInfo> onClick)
		{
			_info = info;
			_onClick = onClick;

			// TODO: Display mutator icons
			_gameModeText.text = _info.Entry.GameModeId.ToUpper();
			_matchTypeText.text = _info.Entry.MatchType.GetTranslation();
			_timeLeftText.gameObject.SetActive(!_info.IsFixed);
		}

		private void Update()
		{
			if (_info.IsFixed) return;

			var timeLeft = _info.EndTime - DateTime.UtcNow;
			_timeLeftText.text = timeLeft.ToString(@"hh\:mm\:ss");
		}

		private void OnDestroy()
		{
			_tooltipButton.onClick.RemoveAllListeners();
		}

		private void OnButtonClick()
		{
			_onClick(_info);
		}

		private void OnTooltipButtonClick()
		{
			var tooltip = _info.Entry.MatchType switch
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