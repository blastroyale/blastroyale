using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Presenters
{

	/// <summary>
	/// This Presenter handles the character dialog system
	/// </summary>
	public class CharacterDialogScreenPresenter : UIPresenter2
	{
		private const string CHARACTER_LEFT = "back_avatar--left";
		private const string CHARACTER_RIGHT = "back_avatar--right";
		private const string CHARACTER_BOTTOM = "back_avatar--bottom";
		private const string CHARACTER_TOP = "back_avatar--top";
		private const string CHARACTER_CENTER = "back_avatar--center";
		
		private const string MOOD_STYLE = "sprite-ftue__character-";

		private const string BUBBLE_LEFT = "bubble--left";
		private const string BUBBLE_RIGHT = "bubble--right";
		private const string BUBBLE_BOTTOM = "bubble--bottom";
		private const string BUBBLE_TOP = "bubble--top";
		private const string BUBBLE_CENTER = "bubble--center";
		
		private const string LOC_TEXT_RIGHT = "bubble__text--right";

		private const int SCALE_DURATION_MS = 250;
		private const int SCALE_BUMP_DURATION_MS = 150;
		private const float BUMP_SIZE = 1.15f;
		
		private VisualElement _backAvatarMale;
		private VisualElement _characterMale;
		private VisualElement _bubbleMale;
		private Label _localizedLabelMale;
		
		private VisualElement _backAvatarFemale;
		private VisualElement _characterFemale;
		private VisualElement _bubbleFemale;
		private Label _localizedLabelFemale;

		private Dictionary<CharacterType, VisualElement[]> _characters; // 0 char VE, 1 bubble, 2 locText, backAvatar

		private IGameServices _services;

		void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_backAvatarMale = Root.Q<VisualElement>("BackAvatarMale").Required();
			_characterMale = Root.Q<VisualElement>("MaleCharacter").Required();
			_bubbleMale = Root.Q<VisualElement>("MaleBubble").Required();
			_localizedLabelMale = Root.Q<Label>("MaleLabel").Required();

			_backAvatarFemale = Root.Q<VisualElement>("BackAvatarFemale").Required();
			_characterFemale = Root.Q<VisualElement>("FemaleCharacter").Required();
			_bubbleFemale = Root.Q<VisualElement>("FemaleBubble").Required();
			_localizedLabelFemale = Root.Q<Label>("FemaleLabel").Required();

			//setup ref dictionary
			_characters = new Dictionary<CharacterType, VisualElement[]>
			{
				{CharacterType.Female, new[] {_characterFemale, _bubbleFemale, _localizedLabelFemale, _backAvatarFemale}},
				{CharacterType.Male, new[] {_characterMale, _bubbleMale, _localizedLabelMale, _backAvatarMale}}
			};
			
			Root.SetupClicks(_services);
		}

		/// <summary>
		/// Plays Appear animation to show dialog box
		/// </summary>
		public void ShowDialog(string message, CharacterType character, CharacterDialogMoodType mood,
							   CharacterDialogPosition position)
		{
			SetMood(character, mood);
			SetPosition(character, position);
			SetText(character, message);
			AnimCharacterIn(character, position);
		}

		/// <summary>
		/// Continues the dialog without animation
		/// </summary>
		public void ContinueDialog(string message, CharacterType character, CharacterDialogMoodType mood)
		{
			SetMood(character, mood);
			SetText(character, message, true);
		}

		/// <summary>
		/// Hides the character and bubble with scaling anim
		/// </summary>
		public void HideDialog(CharacterType character)
		{
			_characters[character][3].experimental.animation.Start((e) => e.transform.scale, new Vector3(0, 0, 1),
				SCALE_DURATION_MS, (e, v) => { e.transform.scale = v; }).OnCompleted(() =>
			{
				_characters[character][3].SetDisplay(false);
				RemovePosStyles(character);
				_characters[character][0].RemoveSpriteClasses();
				
			});

			_characters[character][1].experimental.animation.Start((e) => e.transform.scale, new Vector3(0, 0, 1),
				SCALE_DURATION_MS, (e, v) => { e.transform.scale = v; }).OnCompleted(() =>
			{
				_characters[character][1].SetDisplay(false);
			});
		}

		private void AnimCharacterIn(CharacterType character, CharacterDialogPosition dialogPos)
		{
			Vector3 objScale =
				dialogPos != CharacterDialogPosition.BottomRight && dialogPos != CharacterDialogPosition.TopRight
					? Vector2.one
					: new Vector2(-1, 1);

			_characters[character][3].style.scale = new StyleScale(new Scale(Vector3.zero));
			_characters[character][3].SetDisplay(true);
			_characters[character][3].experimental.animation.Start((e) => e.transform.scale, objScale, SCALE_DURATION_MS,
				(e, v) => { e.transform.scale = v; });

			_characters[character][1].style.scale = new StyleScale(new Scale(Vector3.zero));
			_characters[character][1].SetDisplay(true);
			_characters[character][1].experimental.animation.Start((e) => e.transform.scale, objScale, SCALE_DURATION_MS,
				(e, v) => { e.transform.scale = v; });
		}

		private void SetMood(CharacterType character, CharacterDialogMoodType mood)
		{
			var moodClass = MOOD_STYLE+character.ToString().ToLower() + "-" + mood.ToString().ToLower();
			if (_characters[character][0].ClassListContains(moodClass))
				return;
			
			_characters[character][0].RemoveSpriteClasses();
			
			_characters[character][0].AddToClassList(moodClass);
			
			SmallBumpAnimElement(_characters[character][3]);
		}

		private void SetPosition(CharacterType character, CharacterDialogPosition position)
		{
			AdjustPosStyle(character, position);
		}

		private void AdjustPosStyle(CharacterType character, CharacterDialogPosition position)
		{
			RemovePosStyles(character);

			if (position is CharacterDialogPosition.TopRight or CharacterDialogPosition.BottomRight)
			{
				_characters[character][3].AddToClassList(CHARACTER_RIGHT);
				_characters[character][2].AddToClassList(LOC_TEXT_RIGHT);
				_characters[character][1].AddToClassList(BUBBLE_RIGHT);
			}
			else if (position is CharacterDialogPosition.TopLeft or CharacterDialogPosition.BottomLeft)
			{
				_characters[character][3].AddToClassList(CHARACTER_LEFT);
				_characters[character][1].AddToClassList(BUBBLE_LEFT);
			} 
			else
			{
				_characters[character][3].AddToClassList(CHARACTER_CENTER);
				_characters[character][1].AddToClassList(BUBBLE_CENTER);
			}
			
			if (position is CharacterDialogPosition.TopRight or CharacterDialogPosition.TopLeft)
			{
				_characters[character][3].AddToClassList(CHARACTER_TOP);
				_characters[character][1].AddToClassList(BUBBLE_TOP);
			}
			else if (position is CharacterDialogPosition.BottomLeft or CharacterDialogPosition.BottomRight)
			{
				_characters[character][3].AddToClassList(CHARACTER_BOTTOM);
				_characters[character][1].AddToClassList(BUBBLE_BOTTOM);
			}
			else
			{
				_characters[character][3].AddToClassList(CHARACTER_CENTER);
			}
		}
		
		private void RemovePosStyles(CharacterType character)
		{
			_characters[character][3].RemoveFromClassList(CHARACTER_LEFT);
			_characters[character][3].RemoveFromClassList(CHARACTER_RIGHT);
			_characters[character][3].RemoveFromClassList(CHARACTER_TOP);
			_characters[character][3].RemoveFromClassList(CHARACTER_BOTTOM);
			_characters[character][3].RemoveFromClassList(CHARACTER_CENTER);
			_characters[character][1].RemoveFromClassList(BUBBLE_LEFT);
			_characters[character][1].RemoveFromClassList(BUBBLE_RIGHT);
			_characters[character][1].RemoveFromClassList(BUBBLE_TOP);
			_characters[character][1].RemoveFromClassList(BUBBLE_BOTTOM);
			_characters[character][1].RemoveFromClassList(BUBBLE_CENTER);
			_characters[character][2].RemoveFromClassList(LOC_TEXT_RIGHT);
		}

		private void SetText(CharacterType character, string message, bool bump = false)
		{
			((Label) _characters[character][2]).text = message;
			
			if (bump)
			{
				SmallBumpAnimElement(_characters[character][1]);
			}
		}

		private void SmallBumpAnimElement(VisualElement ve)
		{
			var currentScale = ve.transform.scale;
			ve.experimental.animation.Start((e) => e.transform.scale, currentScale * BUMP_SIZE, SCALE_BUMP_DURATION_MS/2,
				(e, v) => { e.transform.scale = v; }).OnCompleted(() =>
			{
				ve.experimental.animation.Start((e) => e.transform.scale, currentScale, SCALE_BUMP_DURATION_MS/2,
					(e, v) => { e.transform.scale = v; });
			});
		}
	}
	
	public enum CharacterType
	{
		Male,
		Female
	}

	public enum CharacterDialogMoodType
	{
		Happy,
		Neutral,
		Thinking,
		Shocked
	}

	public enum CharacterDialogPosition
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
		Center
	}
	
	
}