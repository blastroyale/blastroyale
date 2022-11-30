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
		private Label _gained;
		private Label _total;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_gained = _root.Q<Label>("Gained").Required();
			_total = _root.Q<Label>("Total").Required();
		}

		/// <summary>
		/// Set the reward data
		/// </summary>
		public void SetData(int gained, int total)
		{
			_gained.text = "+" + gained;
			_total.text = total.ToString();
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}