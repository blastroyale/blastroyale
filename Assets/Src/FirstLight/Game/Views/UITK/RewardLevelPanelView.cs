using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// View for the simple rewards in the rewards match end screen
	/// </summary>
	public class RewardLevelPanelView : UIView
	{
		public struct LevelLevelRewardInfo
		{
			public int NextLevel;
			public int MaxForLevel;
			public int Start;
			public int Total;
			public int MaxLevel;
		}
		
		private Label _gainedLabel;
		private Label _nextLevelLabel;
		private Label _toLevelLabel;
		private Label _totalLabel;
		private VisualElement _previousPointsBar;
		private VisualElement _newPointsBar;
		private Label _next;
		private Label _gainedWeek;
		private Label _totalWeek;
		
		private List<LevelLevelRewardInfo> _levelRewardsInfo;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_gainedLabel = element.Q<Label>("Gained").Required();
			_nextLevelLabel = element.Q<Label>("Level").Required();
			_totalLabel = element.Q<Label>("Total").Required();
			_previousPointsBar = element.Q<VisualElement>("GreenBar").Required();
			_newPointsBar = element.Q<VisualElement>("YellowBar").Required();

			_gainedWeek = element.Q<Label>("GainedWeek");
			_next = element.Q<Label>("Next");
			_totalWeek = element.Q<Label>("TotalWeek");
			_toLevelLabel = element.Q<Label>("ToLevel");
			
			HidePanel();
		}

		/// <summary>
		/// Set the reward data with pool info
		/// </summary>
		public void SetData(List<LevelLevelRewardInfo> levelRewardsInfo, int currentPool, int maxPool, ResourcePoolInfo poolInfo)
		{
			_levelRewardsInfo = levelRewardsInfo;

			_gainedWeek.text = currentPool.ToString();
			_totalWeek.text = "/" + maxPool;

			UpdatePool(poolInfo);
		}
		
		/// <summary>
		/// Set the reward data without pool info
		/// </summary>
		public void SetData(List<LevelLevelRewardInfo> levelRewardsInfo)
		{
			_levelRewardsInfo = levelRewardsInfo;
		}

		/// <summary>
		/// Hides the next level number. Good for level icons that already display current level to avoid confusion.
		/// </summary>
		public void HideFinalLevel()
		{
			_nextLevelLabel.SetDisplay(false);
		}
		
		/// <summary>
		/// Animates the values up to the total
		/// </summary>
		public async Task Animate()
		{
			ShowPanel();

			var currentGained = 0;

			_gainedLabel.text = "0";

			var increaseNumber = 1;

			foreach (var levelRewardInfo in _levelRewardsInfo)
			{
				var levelGained = levelRewardInfo.Start;

				_nextLevelLabel.text = (levelRewardInfo.NextLevel+1).ToString();
				_totalLabel.text = levelGained + "/" + levelRewardInfo.MaxForLevel;
				var nextPointsPercentage = (int)(100 * ((float) (levelRewardInfo.Start+levelRewardInfo.Total) / levelRewardInfo.MaxForLevel));
				_newPointsBar.style.width = Length.Percent(nextPointsPercentage);
				var previousPointsPercentage = (int)(100 * ((float) levelGained / levelRewardInfo.MaxForLevel));
				_previousPointsBar.style.width = Length.Percent(previousPointsPercentage);

				var maxBppForLevel = Math.Min(levelRewardInfo.Start + levelRewardInfo.Total, levelRewardInfo.MaxForLevel);
				
				while (levelGained < maxBppForLevel)
				{
					// TODO: Make this work based on the current framerate
					await Task.Delay(20);
					
					var increase = Math.Min(levelGained + increaseNumber, levelRewardInfo.Start+levelRewardInfo.Total) - levelGained;
					levelGained += increase;
					currentGained += increase;

					_gainedLabel.text = "+" + currentGained;
					_totalLabel.text = levelGained + "/" + levelRewardInfo.MaxForLevel;

					previousPointsPercentage = (int)(100 * ((float) levelGained / levelRewardInfo.MaxForLevel));
					_previousPointsBar.style.width = Length.Percent(previousPointsPercentage);
				}

				if (levelRewardInfo.NextLevel >= levelRewardInfo.MaxLevel)
				{
					_nextLevelLabel.text = ScriptLocalization.UITBattlePass.max;
					_totalLabel.text = ScriptLocalization.UITBattlePass.max;
					_toLevelLabel.SetDisplay(false);
					_previousPointsBar.style.width = Length.Percent(100);
					_newPointsBar.style.width = Length.Percent(100);
				}
			}
		}
		
		private void UpdatePool(ResourcePoolInfo poolInfo)
		{
			var timeLeft = poolInfo.NextRestockTime - DateTime.UtcNow;

			if (poolInfo.IsFull)
			{
				_next.text = string.Empty;
			}
			else
			{
				_next.text = string.Format(ScriptLocalization.UITHomeScreen.resource_pool_restock,
					poolInfo.RestockPerInterval,
					GameId.BPP.ToString(),
					timeLeft.ToHoursMinutesSeconds());
			}
		}

		private void ShowPanel()
		{
			Element.RemoveFromClassList("hidden-reward");
		}
		private void HidePanel()
		{
			Element.AddToClassList("hidden-reward");
		}
	}
}