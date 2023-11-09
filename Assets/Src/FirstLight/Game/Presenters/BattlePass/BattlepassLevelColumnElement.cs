using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.BattlePass
{
	/// <summary>
	/// Displays a glint over an element. Must be used with a vector mask for shaped glints.
	/// </summary>
	public class BattlepassLevelColumnElement : VisualElement
	{
		private const string UssBarGray = "bar__level--gray";

		// Bar stuff
		private VisualElement _completedBar;
		private Label _number;
		private VisualElement _barLevel;

		public BattlepassSegmentButtonElement PaidReward { get; private set; }
		public BattlepassSegmentButtonElement FreeReward { get; private set; }


		public BattlepassLevelColumnElement()
		{
			// TODO: Move to unitask and addressables
			var a = Resources.Load<VisualTreeAsset>("BattlePassLevelColumnElement");
			a.CloneTree(this);
			PaidReward = this.Q<BattlepassSegmentButtonElement>("PaidReward").Required();
			FreeReward = this.Q<BattlepassSegmentButtonElement>("FreeReward").Required();
			QueryBar();
		}
		
		

		private void QueryBar()
		{
			var element = this.Q<VisualElement>("BattlePassLevelBar").Required();
			_completedBar = element.Q("CompletedBar").Required();
			_number = element.Q<Label>("BarLevelLabel").Required();
			_barLevel = element.Q("BarLevel").Required();
		}

		public void SetBarData(uint level, float pctFilled)
		{
			_completedBar.style.width = Length.Percent(pctFilled * 100);
			_number.text = level.ToString();
			if (pctFilled < 1) _barLevel.AddToClassList(UssBarGray);
		}

		public new class UxmlFactory : UxmlFactory<BattlepassLevelColumnElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		
		}
	}
}