using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Presenters.BattlePass
{
	/// <summary>
	/// Displays a glint over an element. Must be used with a vector mask for shaped glints.
	/// </summary>
	public class BattlepassLevelColumnElement : VisualElement
	{
		private const string USS_BAR_GRAY = "bar__level--gray";

		// Bar stuff
		private VisualElement _completedBar;
		private VisualElement _currentLevelArrow;
		private Label _number;
		private Label _priceLabel;
		private VisualElement _barLevel;
		private ImageButton _buyLevelButton;

		public BattlepassSegmentButtonElement PaidReward { get; private set; }
		public BattlepassSegmentButtonElement FreeReward { get; private set; }

		public uint Level { get; private set; }
		public event Action OnBuyLevelClicked;

		public BattlepassLevelColumnElement()
		{
			// TODO: Move to unitask and addressables
			var a = Resources.Load<VisualTreeAsset>("BattlePassLevelColumnElement");
			a.CloneTree(this);
			PaidReward = this.Q<BattlepassSegmentButtonElement>("PaidReward").Required();
			FreeReward = this.Q<BattlepassSegmentButtonElement>("FreeReward").Required();
			QueryBar();
			//DisablePaid();
		}


		private void QueryBar()
		{
			var element = this.Q<VisualElement>("BattlePassLevelBar").Required();
			_completedBar = element.Q("CompletedBar").Required();
			_number = element.Q<Label>("BarLevelLabel").Required();
			_barLevel = element.Q("BarLevel").Required();
			_buyLevelButton = element.Q<ImageButton>("BuyLevelButton").Required();
			_currentLevelArrow = element.Q<VisualElement>("BuyLevelArrow").Required();
			_priceLabel = element.Q<Label>("PriceLabel").Required();
			_buyLevelButton.clicked += () =>
			{
				OnBuyLevelClicked?.Invoke();
			};
		}

		public void DisablePaid()
		{
			this.PaidReward.SetDisplay(false);
		}
		
		public void SetBarData(uint level, bool completed, bool currentLevel, uint buyLevelPrice)
		{
			Level = level;
			_completedBar.style.width = Length.Percent(completed ? 100 : 0);
			_number.text = level.ToString();
			_barLevel.EnableInClassList(USS_BAR_GRAY, !completed);
			_buyLevelButton.SetVisibility(currentLevel && level > 1 && buyLevelPrice > 0);
			_priceLabel.text = buyLevelPrice.ToString();
			if (!currentLevel)
			{
				return;
			}

			_completedBar.style.width = Length.Percent(50);
			AnimateArrow();
		}

		public void AnimateArrow()
		{
			_currentLevelArrow.AnimateTransform(new UnityEngine.Vector3(0, -25), 1000, true, Easing.InOutBack);
		}

		public new class UxmlFactory : UxmlFactory<BattlepassLevelColumnElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}