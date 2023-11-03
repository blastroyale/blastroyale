using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{

	/// <summary>
	/// This class manages the visual elements of battle pass segments on the battle pass screen
	/// </summary>
	public class BattlePassLevelBarView : UIView
	{
		private const string UssBarGray = "bar__level--gray";
		private BattlePassSegmentBarData _data;
		private VisualElement _completedBar;
		private VisualElement _barLevel;
		private Label _number;
		
		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_completedBar = element.Q("CompletedBar").Required();
			_number = element.Q<Label>("BarLevelLabel").Required();
			_barLevel = element.Q("BarLevel").Required();
		}

		public void SetData(in BattlePassSegmentBarData data)
		{
			_data = data;
			_completedBar.style.width = Length.Percent(data.PctFilled * 100);
			_number.text = data.SegmentLevel.ToString();
			if(data.PctFilled < 1) _barLevel.AddToClassList(UssBarGray);
		}
	}

	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public struct BattlePassSegmentBarData
	{
		public uint SegmentLevel;
		public float PctFilled;
	}
}