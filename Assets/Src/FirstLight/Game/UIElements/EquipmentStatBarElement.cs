using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
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

		private readonly VisualElement[] _progressSlices;
		private readonly Label _title;
		private readonly Label _amount;

		private static readonly Dictionary<EquipmentStatType, float> MAX_VALUES = new()
		{
			{EquipmentStatType.Power, 1400},
			{EquipmentStatType.Hp, 1000},
			{EquipmentStatType.Speed, 45f},
			{EquipmentStatType.AttackCooldown, 2f},
			{EquipmentStatType.Armor, 0.10f},
			{EquipmentStatType.ProjectileSpeed, 20},
			{EquipmentStatType.TargetRange, 15f},
			{EquipmentStatType.MaxCapacity, 200},
			{EquipmentStatType.ReloadSpeed, 4f},
			{EquipmentStatType.MinAttackAngle, 60},
			{EquipmentStatType.MaxAttackAngle, 60},
			{EquipmentStatType.SplashDamageRadius, 4f},
			{EquipmentStatType.PowerToDamageRatio, 2f},
			{EquipmentStatType.NumberOfShots, 10},
			{EquipmentStatType.PickupSpeed, 0.25f},
			{EquipmentStatType.ShieldCapacity, 800},
		};

		private static readonly HashSet<EquipmentStatType> INVERT_VALUES = new()
		{
			EquipmentStatType.AttackCooldown,
			EquipmentStatType.MaxAttackAngle,
			EquipmentStatType.MinAttackAngle
		};

		public EquipmentStatBarElement()
		{
			AddToClassList(UssBlock);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(UssBg);

			var progress = new VisualElement {name = "progress"};
			Add(progress);
			progress.AddToClassList(UssProgressBg);

			_progressSlices = new VisualElement[SLICES];
			for (int i = 0; i < SLICES; i++)
			{
				var division = new VisualElement();
				progress.Add(division);
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
			_amount.text = value.ToString(GetValueFormat(type));

			var percentage = value / MAX_VALUES[type];
			if (INVERT_VALUES.Contains(type))
			{
				percentage = 1f - percentage;
			}

			for (int i = 0; i < SLICES; i++)
			{
				_progressSlices[i].style.visibility =
					percentage >= (float) (i + 1) / SLICES ? Visibility.Visible : Visibility.Hidden;
			}
		}

		public void SetUpgradeValue(EquipmentStatType type, float currentValue, float nextValue)
		{
			SetValue(type, currentValue);
			
			// TODO: Set next value
		}

		public static bool CanShowStat(EquipmentStatType type, float value)
		{
			if (!MAX_VALUES.TryGetValue(type, out var maxValue)) return false;

			if (INVERT_VALUES.Contains(type))
			{
				return Mathf.Approximately(value, maxValue);
			}

			return value != 0f;
		}

		private static string GetValueFormat(EquipmentStatType type)
		{
			return type switch
			{
				EquipmentStatType.ReloadSpeed        => "N2",
				EquipmentStatType.PowerToDamageRatio => "P2",
				EquipmentStatType.Armor              => "P2",
				EquipmentStatType.AttackCooldown     => "N2",
				EquipmentStatType.TargetRange        => "N3",
				EquipmentStatType.PickupSpeed        => "P2",
				EquipmentStatType.Speed              => "N2",
				EquipmentStatType.SplashDamageRadius => "N2",
				_                                    => "N0"
			};
		}

		public new class UxmlFactory : UxmlFactory<EquipmentStatBarElement, UxmlTraits>
		{
		}
	}
}