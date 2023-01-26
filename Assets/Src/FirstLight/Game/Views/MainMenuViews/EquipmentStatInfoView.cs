using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View populates the EquipmentDialog with stats for a specified piece of equipment, e.g. Damage, HP Gain, etc.
	/// Each piece of equipment has slightly different modifiers, so this view is used to accomodate the data that it recieves.
	/// </summary>
	public class EquipmentStatInfoView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _statText;
		[SerializeField, Required] private TextMeshProUGUI _valueText;
		[SerializeField, Required] private TextMeshProUGUI _valueTextComparison;
		[SerializeField, Required] private Slider _slider;
		[SerializeField, Required] private Slider _comparisonSlider;
		[SerializeField, Required] private Image _comparisonSliderFillImage;
		[SerializeField] private Color _positiveColor;
		[SerializeField] private Color _negativeColor;
		[SerializeField] private Color _neutralColor;

		private IGameServices _services;

		private readonly Dictionary<EquipmentStatType, float> _maxValuesDictionary = EquipmentExtensions.MAX_VALUES;

		/// <summary>
		/// Set the information of this specific item.
		/// </summary>
		public virtual void SetInfo(EquipmentStatType statType, string statText, float value, float maxValue, string format)
		{
			_statText.text = statText;
			_valueText.text = value.ToString(format);
			_valueTextComparison.text = "";

			if (value > 0 && maxValue > 0)
			{
				_slider.value = value / _maxValuesDictionary[statType];
			}

			_comparisonSlider.gameObject.SetActive(false);
		}

		/// <summary>
		/// Set the information of this specific item, with comparison text.
		/// </summary>
		public void SetComparisonInfo(string statText, string valueText, float delta, EquipmentStatType statType, float value, float comparisonValue)
		{
			_statText.text = statText;
			_valueText.text = valueText;

			if (value > 0)
			{
				_slider.value = value / _maxValuesDictionary[statType];
			}
			else
			{
				_slider.value = 0;
			}
			_valueTextComparison.gameObject.SetActive(false);
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
	}
}