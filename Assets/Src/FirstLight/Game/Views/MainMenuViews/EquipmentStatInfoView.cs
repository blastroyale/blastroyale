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
		
		private readonly Dictionary<EquipmentStatType, float> _maxValuesDictionary = new Dictionary<EquipmentStatType, float>
		{
			{ EquipmentStatType.Damage, 3000 },
			{ EquipmentStatType.Hp, 400 },
			{ EquipmentStatType.Speed, 0.8f },
			{ EquipmentStatType.AttackCooldown, 150 },
			{ EquipmentStatType.Armor, 32 },
			{ EquipmentStatType.ProjectileSpeed, 50 },
			{ EquipmentStatType.TargetRange, 16 },
			{ EquipmentStatType.MaxCapacity, 80 },
			{ EquipmentStatType.ReloadSpeed, 4 },
		};

		/// <summary>
		/// Set the information of this specific item.
		/// </summary>
		public virtual void SetInfo(EquipmentStatType statType, string statText, float value, float maxValue)
		{
			var format = statType == EquipmentStatType.ReloadSpeed ? "N1" : "N0";

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
			
			var format = statType == EquipmentStatType.ReloadSpeed ? "N1" : "N0";
			var textString = "";

			if (delta > 0)
			{
				_valueTextComparison.color = _positiveColor;
				textString = "+" + delta.ToString(format);
			}
			else if (delta < 0)
			{
				_valueTextComparison.color = _negativeColor;
				textString = delta.ToString(format);
			}
			else
			{
				_valueTextComparison.color = _neutralColor;
				textString = delta.ToString(format);
			}
			
			_valueTextComparison.text = textString;
			_valueTextComparison.gameObject.SetActive(true);
			
			if (value > 0)
			{
				_slider.value = value / _maxValuesDictionary[statType];
			}
			else
			{
				_slider.value = 0;
			}
			
			if (comparisonValue > 0)
			{
				_comparisonSlider.gameObject.SetActive(true);
				_comparisonSlider.value = comparisonValue / _maxValuesDictionary[statType];
				_comparisonSliderFillImage.color = _valueTextComparison.color;
			}
			else
			{
				_comparisonSlider.value = 0;
			}
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
	}
}