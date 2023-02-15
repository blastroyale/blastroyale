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
		
		private const string BLOCKER_ELEMENT_STYLE = "blocker-element-blocker";
		private const string HIGHLIGHT_ELEMENT_STYLE = "highlight-element";
		private const string PARENT_ELEMENT_STYLE = "blocker-root";
		
		private IGameServices _services;
		

		protected override void QueryElements(VisualElement root)
		{
			Root.AddToClassList(PARENT_ELEMENT_STYLE);
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
		}-


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