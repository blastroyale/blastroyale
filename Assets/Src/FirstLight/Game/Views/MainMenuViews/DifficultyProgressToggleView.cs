using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View is used on buttons that have different visuals to possible visible states 
	/// </summary>
	public class DifficultyProgressToggleView : MonoBehaviour
	{
		[SerializeField] private AdventureDifficultyLevel _progressionFilter;
		[SerializeField] private UiButtonView _button;
		[SerializeField] private Image _lockImage;
		[SerializeField] private TextMeshProUGUI _filterText;
		[SerializeField] private Image _selectedImage;
		[SerializeField] private Image _selectedBorder;
		[SerializeField] private Image _buttonImage;
		[SerializeField] private GameObject _greyscaleCover;
		
		private IMainMenuServices _mainMenuServices;
		private IGameDataProvider _gameDataProvider;

		private AdventureInfo _info;
		
		/// <summary>
		/// Triggered when the button is clicked and passing the <see cref="AdventureDifficultyLevel"/> of this item referencing the button
		/// </summary>
		public UnityEvent<AdventureDifficultyLevel> OnClickEvent = new UnityEvent<AdventureDifficultyLevel>();

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_lockImage.enabled = false;
			_selectedImage.enabled = false;
			_selectedBorder.enabled = false;
			
			switch (_progressionFilter)
			{
				case AdventureDifficultyLevel.Normal: _filterText.color = Color.green; break;
				case AdventureDifficultyLevel.Hard: _filterText.color = Color.yellow; break;
				case AdventureDifficultyLevel.Master: _filterText.color = Color.red; break;
			}

			_button.onClick.AddListener(OnClick);
		}

		/// <summary>
		/// Set's this view information
		/// </summary>
		public void SetInfo(AdventureDifficultyLevel selectedFilter, AllAdventuresInfo allAdventuresInfo)
		{
			_info = allAdventuresInfo.GetStartIdInfo(_progressionFilter);
			_selectedImage.enabled = _progressionFilter == selectedFilter;
			_selectedBorder.enabled = _progressionFilter == selectedFilter;
			_lockImage.enabled = !_info.IsUnlocked;
			_buttonImage.enabled = _info.IsUnlocked;
			_greyscaleCover.SetActive(!_info.IsUnlocked);
		}

		private void OnClick()
		{
			// Do we need to complete an easier difficulty first?
			if (!_info.IsUnlocked && _progressionFilter != AdventureDifficultyLevel.TOTAL)
			{
				var config = _gameDataProvider.AdventureDataProvider.GetInfo(_info.Config.UnlockedAdventureRequirement).Config;
				var position = transform.position;
				var unlockString = string.Format(ScriptLocalization.MainMenu.BeatLevelFirstWithDifficulty,
				                                 config.Difficulty.ToString().ToUpper(),
				                                 config.Chapter.ToString(),
				                                 config.Stage.ToString());

				position.x += 100;
				
				_mainMenuServices.UiVfxService.PlayFloatingTextAtPosition(unlockString, position);
				
				return;
			}
			
			OnClickEvent.Invoke(_progressionFilter);
		}
	}
}

