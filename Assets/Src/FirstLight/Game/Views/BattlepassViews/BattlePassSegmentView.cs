using Com.TheFallenGames.OSA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.BattlePassViews
{
	public class BattlePassSegmentView : BaseItemViewsHolder
	{
		public TextMeshProUGUI RewardText;
		public Image RewardImage;

		// Class containing the data associated with an item
		public class DataModel
		{
			public string RewardName;
			public Sprite RewardSprite;
		}
	}
}