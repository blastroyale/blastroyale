using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class EquipmentStatBarElement : VisualElement
	{
		private const int SLICES = 10;

		private const string UssBlock = "equipment-stat";

		private const string UssTitleLabel = UssBlock + "__title-label";
		private const string UssAmountLabel = UssBlock + "__amount-label";
		private const string UssBg = UssBlock + "__bg";
		private const string UssProgressBg = UssBlock + "__progress-bg";
		private const string UssProgressSlice = UssBlock + "__progress-slice";

		private VisualElement _progress;
		private VisualElement[] _progressSlices;
		private Label _title;
		private Label _amount;
		
		private static readonly Dictionary<EquipmentStatType, float> MAX_VALUES = new()
		{
			{ EquipmentStatType.Power, 1400 },
			{ EquipmentStatType.Hp, 1000 },
			{ EquipmentStatType.Speed, 45f },
			{ EquipmentStatType.AttackCooldown, 2f },
			{ EquipmentStatType.Armor, 0.10f },
			{ EquipmentStatType.ProjectileSpeed, 20 },
			{ EquipmentStatType.TargetRange, 15f },
			{ EquipmentStatType.MaxCapacity, 200 },
			{ EquipmentStatType.ReloadSpeed, 4f },
			{ EquipmentStatType.MinAttackAngle, 60 },
			{ EquipmentStatType.MaxAttackAngle, 60 },
			{ EquipmentStatType.SplashDamageRadius, 4f },
			{ EquipmentStatType.PowerToDamageRatio, 2f },
			{ EquipmentStatType.NumberOfShots, 10 },
			{ EquipmentStatType.PickupSpeed, 0.25f },
			{ EquipmentStatType.ShieldCapacity, 800 },
		};

		public EquipmentStatBarElement()
		{
			AddToClassList(UssBlock);

			var background = new VisualElement() {name = "background"};
			Add(background);
			background.AddToClassList(UssBg);

			Add(_progress = new VisualElement() {name = "progress"});
			_progress.AddToClassList(UssProgressBg);

			_progressSlices = new VisualElement[SLICES];
			for (int i = 0; i < SLICES; i++)
			{
				var division = new VisualElement();
				_progress.Add(division);
				_progressSlices[i] = division;
				division.AddToClassList(UssProgressSlice);
			}

			Add(_title = new Label("ATTACK") {name = "title"});
			_title.AddToClassList(UssTitleLabel);

			Add(_amount = new Label("269") {name = "amount"});
			_amount.AddToClassList(UssAmountLabel);
		}

		public void SetValue(EquipmentStatType type, float value)
		{
			_title.text = type.GetTranslation();
			_amount.text = Mathf.RoundToInt(value).ToString();

			var percentage = value / MAX_VALUES[type];

			for (int i = 0; i < SLICES; i++)
			{
				_progressSlices[i].style.visibility =
					percentage >= (float) (i + 1) / SLICES ? Visibility.Visible : Visibility.Hidden;
			}
		}

		public static bool CanShowStat(EquipmentStatType type)
		{
			return MAX_VALUES.ContainsKey(type);
		}

		public new class UxmlFactory : UxmlFactory<EquipmentStatBarElement, UxmlTraits>
		{
		}
	}
}