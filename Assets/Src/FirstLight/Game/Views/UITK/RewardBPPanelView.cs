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
		private VisualElement _root;
		private Label _gained;
		private Label _nextLevel;
		private Label _total;
		private VisualElement _previousPointsBar;
		private VisualElement _newPointsBar;
		private Label _gainedWeek;
		private Label _totalWeek;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_gained = _root.Q<Label>("Gained").Required();
			_nextLevel = _root.Q<Label>("Level").Required();
			_total = _root.Q<Label>("Total").Required();
			_previousPointsBar = _root.Q<VisualElement>("GreenBar").Required();
			_newPointsBar = _root.Q<VisualElement>("YellowBar").Required();
			_gainedWeek = _root.Q<Label>("GainedWeek").Required();
			_totalWeek = _root.Q<Label>("TotalWeek").Required();
		}

		/// <summary>
		/// Set the reward data
		/// </summary>
		public void SetData(int gained, int currentTotal, int maxPointsForLevel, int nextLevel, int currentPool, int maxPool)
		{
			_gained.text = (gained > 0 ?"+":"") + gained;
			_nextLevel.text = nextLevel.ToString();
			_total.text = (currentTotal+gained) + "/" + maxPointsForLevel;

			var previousPointsPercentage = (int)(100 * ((float) currentTotal / maxPointsForLevel));
			_previousPointsBar.style.width = Length.Percent(previousPointsPercentage);
			
			var nextPointsPercentage = (int)(100 * ((float) (currentTotal + gained) / maxPointsForLevel));
			_newPointsBar.style.width = Length.Percent(nextPointsPercentage);

			_gainedWeek.text = currentPool.ToString();
			_totalWeek.text = "/" + maxPool;
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}