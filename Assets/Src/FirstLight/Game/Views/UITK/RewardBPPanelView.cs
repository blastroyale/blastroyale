using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// View for the simple rewards in the rewards match end screen
	/// </summary>
	public class RewardBPPanelView : IUIView
	{
		public struct BPPLevelRewardInfo
		{
			public int NextLevel;
			public int MaxForLevel;
			public int Start;
			public int Total;
		}
		
		private VisualElement _root;
		private Label _gainedLabel;
		private Label _nextLevelLabel;
		private Label _totalLabel;
		private VisualElement _previousPointsBar;
		private VisualElement _newPointsBar;
		private Label _gainedWeek;
		private Label _totalWeek;

		private int _gained;
		private List<BPPLevelRewardInfo> _levelRewardsInfo;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_gainedLabel = _root.Q<Label>("Gained").Required();
			_nextLevelLabel = _root.Q<Label>("Level").Required();
			_totalLabel = _root.Q<Label>("Total").Required();
			_previousPointsBar = _root.Q<VisualElement>("GreenBar").Required();
			_newPointsBar = _root.Q<VisualElement>("YellowBar").Required();
			_gainedWeek = _root.Q<Label>("GainedWeek").Required();
			_totalWeek = _root.Q<Label>("TotalWeek").Required();
			
			HidePanel();
		}

		/// <summary>
		/// Set the reward data
		/// </summary>
		public void SetData(int gained, List<BPPLevelRewardInfo> levelRewardsInfo, int currentPool, int maxPool)
		{
			_gained = gained;

			_levelRewardsInfo = levelRewardsInfo;

			_gainedWeek.text = currentPool.ToString();
			_totalWeek.text = "/" + maxPool;
		}
		
		/// <summary>
		/// Animates the values up to the total
		/// </summary>
		public async Task Animate()
		{
			ShowPanel();

			var currentGained = 0;

			_gainedLabel.text = "0";
			

			var increaseNumber = _gained / 150;

			foreach (var levelRewardInfo in _levelRewardsInfo)
			{
				var levelGained = levelRewardInfo.Start;

				_nextLevelLabel.text = (levelRewardInfo.NextLevel+1).ToString();
				_totalLabel.text = levelGained + "/" + levelRewardInfo.MaxForLevel;
				var nextPointsPercentage = (int)(100 * ((float) (levelRewardInfo.Start+levelRewardInfo.Total) / levelRewardInfo.MaxForLevel));
				_newPointsBar.style.width = Length.Percent(nextPointsPercentage);
				var previousPointsPercentage = (int)(100 * ((float) levelGained / levelRewardInfo.MaxForLevel));
				_previousPointsBar.style.width = Length.Percent(previousPointsPercentage);

				while (levelGained < levelRewardInfo.Start+levelRewardInfo.Total)
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
			}
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
		
		private void ShowPanel()
		{
			_root.RemoveFromClassList("hidden-reward");
		}
		private void HidePanel()
		{
			_root.AddToClassList("hidden-reward");
		}
	}
}