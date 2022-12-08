using System;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// View for the simple rewards in the rewards match end screen
	/// </summary>
	public class RewardPanelView : IUIView
	{
		private VisualElement _root;
		private Label _gainedLabel;
		private Label _totalLabel;

		private int _gained;
		private int _total;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_gainedLabel = _root.Q<Label>("Gained").Required();
			_totalLabel = _root.Q<Label>("Total").Required();

			HidePanel();
		}

		/// <summary>
		/// Set the reward data
		/// </summary>
		public void SetData(int gained, int total)
		{
			_gained = gained;
			_total = total;
			
			_gainedLabel.text = "0";
			_totalLabel.text = total.ToString();
		}

		/// <summary>
		/// Animates the values up to the total
		/// </summary>
		public async Task Animate()
		{
			ShowPanel();
			
			if (_gained == 0)
			{
				return;
			}
			
			var currentGained = 0;
			var currentTotal = _total;
			var increment = _gained < 0 ? -1 : 1;

			while (currentGained != _gained)
			{
				await Task.Delay(50);
				currentGained += increment;
				currentTotal += increment;

				_gainedLabel.text = (currentGained < 0 ? "-" : "+") + currentGained;
				_totalLabel.text = Math.Max(0, currentTotal).ToString();
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