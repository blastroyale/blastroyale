using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Presenters
{

	/// <summary>
	/// This Presenter handles the character dialog system
	/// </summary>
	public class CharacterDialogScreenPresenter : UiToolkitPresenter
	{
		private const string CHARACTER_LEFT = "character--left";
		private const string CHARACTER_RIGHT = "character--right";
		private const string BUBBLE_LEFT = "bubble--left";
		private const string BUBBLE_RIGHT = "bubble--right";
		private const string LOC_TEXT_RIGHT = "bubble__localized_text__right";

		private const int SCALE_DURATION_MS = 250;

		private VisualElement _topLeftContainer;
		private VisualElement _topRightContainer;
		private VisualElement _bottomLeftContainer;
		private VisualElement _bottomRightContainer;
		private VisualElement _centerContainer;

		private VisualElement _characterMale;
		private VisualElement _bubbleMale;
		private LocalizedLabel _localizedLabelMale;

		private VisualElement _characterFemale;
		private VisualElement _bubbleFemale;
		private LocalizedLabel _localizedLabelFemale;

		private Dictionary<CharacterType, VisualElement[]> _characters; // 0 char VE, 1 bubble, 2 locText
		private Dictionary<CharacterDialogPosition, VisualElement> _positionsToContainers; // 0 char VE, 1 bubble, 2 locText

		private IGameServices _services;

		void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_topLeftContainer = root.Q<VisualElement>("TopLeftContainer").Required();
			_topRightContainer = root.Q<VisualElement>("TopRightContainer").Required();
			_bottomLeftContainer = root.Q<VisualElement>("BottomLeftContainer").Required();
			_bottomRightContainer = root.Q<VisualElement>("BottomRightContainer").Required();
			_centerContainer = root.Q<VisualElement>("CenterContainer").Required();

			_characterMale = root.Q<VisualElement>("MaleCharacter").Required();
			_bubbleMale = root.Q<VisualElement>("MaleBubble").Required();
			_localizedLabelMale = root.Q<LocalizedLabel>("MaleLocalizedLabel").Required();

			_characterFemale = root.Q<VisualElement>("FemaleCharacter").Required();
			_bubbleFemale = root.Q<VisualElement>("FemaleBubble").Required();
			_localizedLabelFemale = root.Q<LocalizedLabel>("FemaleLocalizedLabel").Required();

			//setup ref dictionaries
			_characters = new Dictionary<CharacterType, VisualElement[]>
			{
				{CharacterType.Female, new[] {_characterFemale, _bubbleFemale, _localizedLabelFemale}},
				{CharacterType.Male, new[] {_characterMale, _bubbleMale, _localizedLabelMale}}
			};

			_positionsToContainers = new Dictionary<CharacterDialogPosition, VisualElement>
			{
				{CharacterDialogPosition.TopLeft, _topLeftContainer},
				{CharacterDialogPosition.TopRight, _topRightContainer},
				{CharacterDialogPosition.BottomLeft, _bottomLeftContainer},
				{CharacterDialogPosition.BottomRight, _bottomRightContainer},
				{CharacterDialogPosition.Center, _centerContainer}
			};

			root.SetupClicks(_services);
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
		public void ContinueDialog(string message, CharacterType character, CharacterDialogMoodType mood,
								   CharacterDialogPosition position)
		{
			SetMood(character, mood);
			SetPosition(character, position);
			SetText(character, message);
		}

		/// <summary>
		/// Hides the character and bubble with scaling anim
		/// </summary>
		public void HideDialog(CharacterType character)
		{
			_characters[character][0].experimental.animation.Start((e) => e.transform.scale, new Vector3(0, 0, 1),
				SCALE_DURATION_MS, (e, v) => { e.transform.scale = v; }).OnCompleted(() =>
			{
				_characters[character][0].SetDisplay(false);
				RemoveFromPosition(character);
				RemovePosStyles(character);
				RemoveMoodStyles(character);
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

			_characters[character][0].style.scale = new StyleScale(new Scale(Vector3.zero));
			_characters[character][0].SetDisplay(true);
			_characters[character][0].experimental.animation.Start((e) => e.transform.scale, objScale, SCALE_DURATION_MS,
				(e, v) => { e.transform.scale = v; });

			_characters[character][1].style.scale = new StyleScale(new Scale(Vector3.zero));
			_characters[character][1].SetDisplay(true);
			_characters[character][1].experimental.animation.Start((e) => e.transform.scale, objScale, SCALE_DURATION_MS,
				(e, v) => { e.transform.scale = v; });
		}

		private void SetMood(CharacterType character, CharacterDialogMoodType mood)
		{
			RemoveMoodStyles(character);
			_characters[character][0].AddToClassList(character.ToString().ToLower() + "_" + mood.ToString().ToLower());
		}

		private void SetPosition(CharacterType character, CharacterDialogPosition position)
		{
			if (IsAtPosition(character, position))
				return;

			AdjustPosStyle(character, position);

			//TODO check if different to current, if so anim out and call anim in on complete

			_positionsToContainers[position].Add(_characters[character][0]);
			_positionsToContainers[position].Add(_characters[character][1]);
		}

		private void AdjustPosStyle(CharacterType character, CharacterDialogPosition position)
		{
			RemovePosStyles(character);

			if (position is CharacterDialogPosition.TopRight or CharacterDialogPosition.BottomRight)
			{
				_characters[character][0].AddToClassList(CHARACTER_RIGHT);
				_characters[character][1].AddToClassList(BUBBLE_RIGHT);
				_characters[character][2].AddToClassList(LOC_TEXT_RIGHT);
			}
			else
			{
				_characters[character][0].AddToClassList(CHARACTER_LEFT);
				_characters[character][1].AddToClassList(BUBBLE_LEFT);
			}
		}

		private bool IsAtPosition(CharacterType character, CharacterDialogPosition position)
		{
			return _positionsToContainers[position].Contains(_characters[character][0]);
		}

		private void RemoveFromPosition(CharacterType character)
		{
			Root.hierarchy.Add(_characters[character][0]);
			Root.hierarchy.Add(_characters[character][1]);
		}

		private void RemovePosStyles(CharacterType character)
		{
			_characters[character][0].RemoveFromClassList(CHARACTER_LEFT);
			_characters[character][0].RemoveFromClassList(CHARACTER_RIGHT);
			_characters[character][1].RemoveFromClassList(BUBBLE_LEFT);
			_characters[character][1].RemoveFromClassList(BUBBLE_RIGHT);
			_characters[character][2].RemoveFromClassList(LOC_TEXT_RIGHT);
		}

		private void SetText(CharacterType character, string message)
		{
			((LocalizedLabel) _characters[character][2]).Localize(message);
		}

		private void RemoveMoodStyles(CharacterType character)
		{
			foreach (CharacterDialogMoodType mood in (CharacterDialogMoodType[]) Enum.GetValues(
						 typeof(CharacterDialogMoodType)))
			{
				_characters[character][0].RemoveFromClassList(String.Concat(character.ToString().ToLower(), "_", mood.ToString().ToLower()));
			}
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