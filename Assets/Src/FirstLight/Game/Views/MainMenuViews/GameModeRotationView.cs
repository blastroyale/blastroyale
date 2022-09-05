using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Displays a rotating game mode - normal game mode button with time left.
	/// </summary>
	public class GameModeRotationView : MonoBehaviour
	{
		[SerializeField] private GameModeButtonView _buttonView;
		[SerializeField] private TextMeshProUGUI _timeLeftText;

		private GameModeRotationInfo _data;

		public void Init(GameModeRotationInfo data, Action<string, List<string>, MatchType, bool, DateTime> onClick)
		{
			_data = data;
			_buttonView.Init(data.Entry.GameModeId, data.Entry.Mutators, MatchType.Casual, true, data.EndTime,
			                 onClick);
		}

		private void Update()
		{
			var timeLeft = _data.EndTime - DateTime.UtcNow;
			_timeLeftText.text = timeLeft.ToString(@"hh\:mm\:ss");
		}
	}
}