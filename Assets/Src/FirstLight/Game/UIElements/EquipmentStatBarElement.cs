using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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
		private const string UssAmountHolder = UssBlock + "__amount-holder";
		private const string UssAmountLabel = UssBlock + "__amount-label";
		private const string UssAmountLabelNext = UssBlock + "__amount-label--next";
		private const string UssAmountArrow = UssBlock + "__amount-arrow";
		private const string UssBg = UssBlock + "__bg";
		private const string UssProgressBg = UssBlock + "__progress-bg";
		private const string UssProgressSlice = UssBlock + "__progress-slice";
		private const string UssProgressSliceGreen = UssProgressSlice + "--green";

		private readonly VisualElement[] _progressSlices;
		private readonly Label _title;
		private readonly Label _amount;
		private readonly VisualElement _amountArrow;
		private readonly Label _amountNext;

		private static readonly Dictionary<EquipmentStatType, float> MAX_VALUES = new()
		{
			{EquipmentStatType.Power, 1400},
			{EquipmentStatType.Hp, 1000},
			{EquipmentStatType.Speed, 45f},
			{EquipmentStatType.AttackCooldown, 2f},
			{EquipmentStatType.Armor, 0.10f},
			{EquipmentStatType.ProjectileSpeed, 20},
			{EquipmentStatType.TargetRange, 15f},
			{EquipmentStatType.MaxCapacity, 120},
			{EquipmentStatType.ReloadTime, 4f},
			{EquipmentStatType.MinAttackAngle, 60},
			{EquipmentStatType.MaxAttackAngle, 60},
			{EquipmentStatType.SplashDamageRadius, 4f},
			{EquipmentStatType.PowerToDamageRatio, 2f},
			{EquipmentStatType.NumberOfShots, 10},
			{EquipmentStatType.PickupSpeed, 0.25f},
			{EquipmentStatType.ShieldCapacity, 800},
			{EquipmentStatType.MagazineSize, 30},
		};

		private static readonly HashSet<EquipmentStatType> INVERT_VALUES = new()
		{
			EquipmentStatType.AttackCooldown,
			EquipmentStatType.MaxAttackAngle,
			EquipmentStatType.MinAttackAngle,
			EquipmentStatType.ReloadTime
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

			var amountHolder = new VisualElement {name = "amount-holder"};
			amountHolder.AddToClassList(UssAmountHolder);
			Add(amountHolder);
			{
				amountHolder.Add(_amount = new Label("269") {name = "amount"});
				_amount.AddToClassList(UssAmountLabel);

				amountHolder.Add(_amountArrow = new VisualElement {name = "arrow"});
				_amountArrow.AddToClassList(UssAmountArrow);

				amountHolder.Add(_amountNext = new Label("290") {name = "amount-next"});
				_amountNext.AddToClassList(UssAmountLabel);
				_amountNext.AddToClassList(UssAmountLabelNext);
			}

			SetValue(EquipmentStatType.Hp, 500,  true, 700);
		}

		public void SetValue(EquipmentStatType type, float currentValue, bool showUpgrade = false, float nextValue = 0f)
		{
			_title.text = type.GetTranslation();
			_amount.text = currentValue.ToString(GetValueFormat(type));
			_amountNext.text = nextValue.ToString(GetValueFormat(type));
			_amountNext.SetDisplay(showUpgrade);
			_amountArrow.SetDisplay(showUpgrade);

			var percentage = currentValue / MAX_VALUES[type];
			var percentageNext = nextValue / MAX_VALUES[type];
			if (INVERT_VALUES.Contains(type))
			{
				percentage = 1f - percentage;
				percentageNext = 1f - percentageNext;
			}

			for (int i = 0; i < SLICES; i++)
			{
				var slice = _progressSlices[i];
				var sliceShown = i == 0 || percentage >= (float) (i + 1) / SLICES;
				var sliceNextShown = percentageNext >= (float) (i + 1) / SLICES;
				
				slice.RemoveModifiers();
				slice.SetVisibility(sliceShown || (showUpgrade && sliceNextShown));

				if (showUpgrade && !sliceShown && sliceNextShown)
				{
					slice.AddToClassList(UssProgressSliceGreen);
				}
			}
		}

		public static bool CanShowStat(EquipmentStatType type, float value)
		{
			if (!MAX_VALUES.TryGetValue(type, out var maxValue)) return false;

			return INVERT_VALUES.Contains(type) || value != 0f;
		}

		private static string GetValueFormat(EquipmentStatType type)
		{
			return type switch
			{
				EquipmentStatType.ReloadTime        => "N2",
				EquipmentStatType.PowerToDamageRatio => "P2",
				EquipmentStatType.Armor              => "P2",
				EquipmentStatType.AttackCooldown     => "N2",
				EquipmentStatType.TargetRange        => "N3",
				EquipmentStatType.PickupSpeed        => "P2",
				EquipmentStatType.Speed              => "N3",
				EquipmentStatType.SplashDamageRadius => "N2",
				EquipmentStatType.MaxCapacity        => "P2",
				_                                    => "N0"
			};
		}

		public new class UxmlFactory : UxmlFactory<EquipmentStatBarElement, UxmlTraits>
		{
		}
	}
}