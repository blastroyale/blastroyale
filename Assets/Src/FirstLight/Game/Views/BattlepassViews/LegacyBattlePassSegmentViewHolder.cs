using Com.TheFallenGames.OSA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.BattlePassViews
{
	/// <summary>
	/// This class is an OSA implementation of a views holder, which holds reference to the actual battle pass segment
	/// </summary>
	public class LegacyBattlePassSegmentViewHolder : BaseItemViewsHolder
	{
		public LegacyBattlePassSegmentView View;
		
		public override void CollectViews()
		{
			base.CollectViews();
			
			View = root.GetComponent<LegacyBattlePassSegmentView>();
		}
	}
}