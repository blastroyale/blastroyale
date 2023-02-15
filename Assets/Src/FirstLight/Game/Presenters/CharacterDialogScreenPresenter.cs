using FirstLight.Game.Utils;
using FirstLight.Game.Services;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public enum CharacterType
	{
		Male, Female
	}
	public enum CharacterDialogMoodType
	{
		Happy, Talking, Confused, Shocked
	}

	public enum CharacterDialogPosition
	{
		TopLeft, TopRight, BottomLeft, BottomRight, Center
	}

	/// <summary>
	/// This Presenter handles the character dialog system
	/// </summary>
	public class CharacterDialogScreenPresenter : UiToolkitPresenter
	{
		float femaleWidht = 1460; 
		float femaleHeight = 1608;
		float maleWidht = 1531; 
		float maleHeight = 1799;


		private VisualElement _characterTopLeft;
		private VisualElement _bubbleTopLeft;
		private VisualElement _characterTopRight;
		private VisualElement _bubbleTopRight;
		private VisualElement _characterBottomLeft;
		private VisualElement _bubbleBottomLeft;
		private VisualElement _characterBottomRight;
		private VisualElement _bubbleBottomRight;
		private VisualElement _characterCenter;
		private VisualElement _bubbleCenter;


		private IGameServices _services;
		

		protected override void QueryElements(VisualElement root)
		{
			_characterTopLeft = root.Q<VisualElement>("CharacterTopLeft").Required();
			_bubbleTopLeft = root.Q<VisualElement>("BubbleTopLeft").Required();
			_characterTopRight = root.Q<VisualElement>("CharacterTopRight").Required();
			_bubbleTopRight = root.Q<VisualElement>("BubbleTopRight").Required();
			_characterBottomLeft = root.Q<VisualElement>("CharacterBottomLeft").Required();
			_bubbleBottomLeft = root.Q<VisualElement>("BubbleBottomLeft").Required();
			_characterBottomRight = root.Q<VisualElement>("CharacterBottomRight").Required();
			_bubbleBottomRight = root.Q<VisualElement>("BubbleBottomRight").Required();
			_characterCenter = root.Q<VisualElement>("CharacterCenter").Required();
			_bubbleCenter = root.Q<VisualElement>("BubbleCenter").Required();

			root.SetupClicks(_services);
		}

		/// <summary>
		/// Plays Appear animation to show dialog box
		/// </summary>
		/// <param name="message"></param>
		/// <param name="character"></param>
		/// <param name="mood"></param>
		/// <param name="position"></param>
		public void ShowDialog(string message, CharacterType character, CharacterDialogMoodType mood,
							   CharacterDialogPosition position)
		{
		}


		/// <summary>
		/// Changes to new message/character mood image. Any "Continue" animation would play if there will be one
		/// </summary>
		/// <param name="message"></param>
		/// <param name="character"></param>
		/// <param name="mood"></param>
		/// <param name="position"></param>
		public void ContinueDialog(string message, CharacterType character, CharacterDialogMoodType mood,
								   CharacterDialogPosition position)
		{
		}

		public void HideDialog(CharacterType character)
		{
			
		}
		
		

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
	
	}
}