using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This view is responsible for switching the game quality visuals
	/// </summary>
	public class DetailLevelToggleView : MonoBehaviour
	{
		[SerializeField] private Button _lowSettingsButton;
		[SerializeField] private Button _mediumSettingsButton;
		[SerializeField] private Button _highSettingsButton;

		public event Action<GraphicsConfig.DetailLevel> ValueChanged;

		private void Awake()
		{
			_lowSettingsButton.onClick.AddListener(OnLowClicked);
			_mediumSettingsButton.onClick.AddListener(OnMediumClicked);
			_highSettingsButton.onClick.AddListener(OnHighClicked);
		}

		/// <summary>
		/// Sets the visual button state based on the given <paramref name="detailLevel"/>
		/// </summary>
		public void SetSelectedDetailLevel(GraphicsConfig.DetailLevel detailLevel)
		{
			ShowSelectedDetailLevel(detailLevel);
			ValueChanged?.Invoke(detailLevel);
		}

		private void OnHighClicked()
		{
			SetSelectedDetailLevel(GraphicsConfig.DetailLevel.High);
		}

		private void OnMediumClicked()
		{
			SetSelectedDetailLevel(GraphicsConfig.DetailLevel.Medium);
		}

		private void OnLowClicked()
		{
			SetSelectedDetailLevel(GraphicsConfig.DetailLevel.Low);
		}

		private void ShowSelectedDetailLevel(GraphicsConfig.DetailLevel detailLevel)
		{
			UpdateButton(_highSettingsButton, detailLevel == GraphicsConfig.DetailLevel.High);
			UpdateButton(_mediumSettingsButton, detailLevel == GraphicsConfig.DetailLevel.Medium);
			UpdateButton(_lowSettingsButton, detailLevel == GraphicsConfig.DetailLevel.Low);
		}

		private void UpdateButton(Button button, bool selected)
		{
			button.image.color = selected ? button.colors.highlightedColor : button.colors.normalColor;
			button.interactable = !selected;
		}
	}
}