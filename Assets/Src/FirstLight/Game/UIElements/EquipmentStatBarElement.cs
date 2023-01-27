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

			SetValue(EquipmentStatType.Hp, 500, true, 700);
		}

		public void SetValue(EquipmentStatType type, float currentValue, bool showUpgrade = false, float nextValue = 0f)
		{
			_title.text = type.GetLocalization();
			_amount.text = currentValue.ToString(EquipmentExtensions.GetValueFormat(type));
			_amountNext.text = nextValue.ToString(EquipmentExtensions.GetValueFormat(type));
			_amountNext.SetDisplay(showUpgrade);
			_amountArrow.SetDisplay(showUpgrade);

			var percentage = currentValue / EquipmentExtensions.MAX_VALUES[type];
			var percentageNext = nextValue / EquipmentExtensions.MAX_VALUES[type];

			if (EquipmentExtensions.INVERT_VALUES.Contains(type))
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
			return EquipmentExtensions.CanShowStat(type, value);
		}


		public new class UxmlFactory : UxmlFactory<EquipmentStatBarElement, UxmlTraits>
		{
		}
	}
}