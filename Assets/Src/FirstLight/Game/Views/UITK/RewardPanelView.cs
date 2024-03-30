using System;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// View for the simple rewards in the rewards match end screen
	/// </summary>
	public class RewardPanelView : UIView2
	{
		private Label _gainedLabel;
		private Label _totalLabel;

		private int _gained;
		private int _total;
		
		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_gainedLabel = element.Q<Label>("Gained").Required();
			_totalLabel = element.Q<Label>("Total").Required();

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
				await Task.Delay(2);
				currentGained += increment;
				currentTotal += increment;

				_gainedLabel.text = (currentGained > 0 ? "+" : "") + currentGained;
				_totalLabel.text = Math.Max(0, currentTotal).ToString();
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