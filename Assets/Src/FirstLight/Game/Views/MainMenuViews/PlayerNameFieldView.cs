using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Player name field - subscribes to player name changes, updates text component.
	/// </summary>
	public class PlayerNameFieldView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _textField;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void OnValidate()
		{
			_textField = _textField == null ? GetComponent<TextMeshProUGUI>() : _textField;
		}

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider.AppDataProvider.DisplayName.InvokeObserve(OnPlayerNameChanged);
		}

		private void OnDestroy()
		{
			_gameDataProvider?.AppDataProvider?.DisplayName?.StopObserving(OnPlayerNameChanged);
		}

		private void OnPlayerNameChanged(string previousValue, string newValue)
		{
			_textField.text = _gameDataProvider.AppDataProvider.DisplayNameTrimmed;
		}
	}
}