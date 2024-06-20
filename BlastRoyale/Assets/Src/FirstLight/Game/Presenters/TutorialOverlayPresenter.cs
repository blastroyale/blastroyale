using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Game.Services;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Tutorial utils
	/// </summary>
	[UILayer(UILayer.TutorialOverlay)]
	public class TutorialOverlayPresenter : UIPresenter
	{
		private const string BLOCKER_ELEMENT_STYLE = "blocker-element-blocker";
		private const string HIGHLIGHT_ELEMENT_STYLE = "highlight-element";
		private const string PARENT_ELEMENT_STYLE = "blocker-root";
		
		private const float CIRCLE_DEFAULT_SIZE = 32;
		private const float SQUARE_DEFAULT_SIZE = 512;
		
		[SerializeField] private GameObject _guideHandRoot;
		[SerializeField] private Animator _guideHandAnimator;

		public CharacterDialogView Dialog;

		private float _initialScale;
		private float _highlightedScale;

		private IGameServices _services;

		private VisualElement _blockerRoot;
		private VisualElement _blockerElementRight;
		private VisualElement _blockerElementLeft;
		private VisualElement _blockerElementBottom;
		private VisualElement _blockerElementTop;

		private VisualElement _highlighterElement;
		
		private readonly float _guideHandRotation = 45; // rotation that already is from the art image
		private float _guideHandRotationDegreeDegreesOffset;

		/// <summary>
		/// Controls the animation rotation so we can
		/// make the hand drag into any direction we think fit
		/// </summary>
		public float RotationDegreeOffset
		{
			get => _guideHandRotationDegreeDegreesOffset;
			set
			{
				_guideHandRotationDegreeDegreesOffset = value;
				_guideHandRoot.transform.rotation = Quaternion.Euler(0, 0, _guideHandRotationDegreeDegreesOffset - _guideHandRotation);
			}
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			Root.AddToClassList(PARENT_ELEMENT_STYLE);
			Root.SetupClicks(_services);

			Root.Q<VisualElement>("Dialog").AttachView(this, out Dialog);
			_blockerRoot = Root.Q("BlockerRoot");
		}

		/// <summary>
		/// Blocks full screen
		/// </summary>
		public void BlockFullScreen()
		{
			_blockerElementRight = new VisualElement();
			_blockerElementRight.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_blockerRoot.Add(_blockerElementRight);
			SetBlockerValues(_blockerElementRight,
				_blockerRoot.resolvedStyle.height * 2,
				_blockerRoot.resolvedStyle.width * 2,
				0,
				0);

			_blockerElementLeft = new VisualElement();
			_blockerRoot.Add(_blockerElementLeft);

			_blockerElementBottom = new VisualElement();
			_blockerRoot.Add(_blockerElementBottom);

			_blockerElementTop = new VisualElement();
			_blockerRoot.Add(_blockerElementTop);
		}

		/// <summary>
		/// Creates blocker elements around ui element object on the <typeparamref name="T"/> presenter.
		/// </summary>
		public async UniTask BlockAround<T>(string className = null, string elementName = null)
			where T : UIPresenter
		{
			await UniTask.WaitUntil(() => _services.UIService.IsScreenOpen<T>());
			var root = _services.UIService.GetScreen<T>().Root;
			var element = root.Q(elementName, className);
			
			CreateBlockers(element);
		}

		public async UniTask EnsurePresenterElement<T>(string className = null, string elementName = null)
			where T : UIPresenter
		{
			await UniTask.WaitUntil(() => _services.UIService.IsScreenOpen<T>());
			var root = _services.UIService.GetScreen<T>().Root;
			var element = root.Q(elementName, className);

			while (element.worldBound is {width: 0, height: 0})
			{
				await UniTask.NextFrame();
			}
		}

		/// <summary>
		/// Removes blockers
		/// </summary>
		/// <returns></returns>
		public void Unblock()
		{
			_blockerElementRight.RemoveFromHierarchy();
			_blockerElementLeft.RemoveFromHierarchy();
			_blockerElementBottom.RemoveFromHierarchy();
			_blockerElementTop.RemoveFromHierarchy();
		}

		/// <summary>
		/// Highligts the ui element object with passed class or name from T, with an animation of a circle shringking. 
		/// </summary>
		/// <param name="className"></param>
		/// <param name="elementName"></param>
		/// <param name="sizeMultiplier"></param>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="Exception"></exception>
		public void Highlight<T>(string className = null, string elementName = null, float sizeMultiplier = 1)
			where T : UIPresenter
		{
			var root = _services.UIService.GetScreen<T>().Root;
			var element = root.Q(elementName, className);
			
			CreateHighlight(element, sizeMultiplier);
		}

		/// <summary>
		///  Removes the highlight circle with an increasing animation. Then destroys the highligh element
		/// </summary>
		public void RemoveHighlight()
		{
			_highlighterElement.experimental.animation.Scale(_initialScale, GameConstants.Tutorial.TIME_HIGHLIGHT_FADE)
				.OnCompleted(DeleteHighLightElement);
		}
		
		public void ShowGuideHand()
		{
			_guideHandRoot.SetActive(true);
			_guideHandAnimator.enabled = true;
		}

		public void HideGuideHand()
		{
			_guideHandRoot.SetActive(false);
			_guideHandAnimator.enabled = false;
		}
		
		public void SetGuideHandScreenPosition(Vector2 screenPosition, float fingerRotation = 45)
		{
			_guideHandRoot.transform.position = screenPosition;
			RotationDegreeOffset = fingerRotation;
			ShowGuideHand();
		}

		private void DeleteHighLightElement()
		{
			_blockerRoot.Remove(_highlighterElement);
		}

		private void CreateBlockers(VisualElement objElement)
		{
			_blockerElementRight = new VisualElement();
			_blockerElementRight.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_blockerRoot.Add(_blockerElementRight);
			SetBlockerValues(_blockerElementRight,
				_blockerRoot.resolvedStyle.height * 2,
				_blockerRoot.resolvedStyle.width,
				objElement.worldBound.y - _blockerRoot.resolvedStyle.height,
				objElement.worldBound.x + objElement.worldBound.width);

			_blockerElementLeft = new VisualElement();
			_blockerElementLeft.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_blockerRoot.Add(_blockerElementLeft);
			SetBlockerValues(_blockerElementLeft,
				_blockerRoot.resolvedStyle.height * 2,
				_blockerRoot.resolvedStyle.width,
				objElement.worldBound.y - _blockerRoot.resolvedStyle.height,
				objElement.worldBound.x - _blockerRoot.worldBound.width);

			_blockerElementBottom = new VisualElement();
			_blockerElementBottom.AddToClassList(BLOCKER_ELEMENT_STYLE);
			_blockerRoot.Add(_blockerElementBottom);
			SetBlockerValues(_blockerElementBottom, _blockerRoot.resolvedStyle.height,
				objElement.resolvedStyle.width,
				objElement.worldBound.y + objElement.worldBound.height,
				objElement.worldBound.x);

			_blockerElementTop = new VisualElement();
			_blockerRoot.Add(_blockerElementTop);
			_blockerElementTop.AddToClassList(BLOCKER_ELEMENT_STYLE);
			SetBlockerValues(_blockerElementTop, _blockerRoot.resolvedStyle.height, objElement.resolvedStyle.width,
				objElement.worldBound.y - _blockerRoot.worldBound.height, objElement.worldBound.x);
		}

		private void SetBlockerValues(VisualElement blocker, float height, float width, float top, float left)
		{
			blocker.style.height = height;
			blocker.style.width = width;
			blocker.style.top = top;
			blocker.style.left = left;
		}

		private void CreateHighlight(VisualElement objElement, float sizeMultiplier)
		{
			_highlighterElement = new VisualElement();

			_blockerRoot.Add(_highlighterElement);
			_highlighterElement.AddToClassList(HIGHLIGHT_ELEMENT_STYLE);
			_highlighterElement.pickingMode = PickingMode.Ignore;
			_highlighterElement.SetDisplay(false);
			float objSize = objElement.resolvedStyle.width >= objElement.resolvedStyle.height
				? objElement.resolvedStyle.width
				: objElement
					.resolvedStyle.height;

			objSize *= sizeMultiplier;

			float circleHighlightingSize = objSize;
			
			_initialScale = _blockerRoot.worldBound.width * 2 / CIRCLE_DEFAULT_SIZE;
			_highlightedScale = circleHighlightingSize / CIRCLE_DEFAULT_SIZE;
			_highlighterElement.style.top = objElement.worldBound.y - SQUARE_DEFAULT_SIZE / 2 + objElement.resolvedStyle.height / 2;
			_highlighterElement.style.left = objElement.worldBound.x - SQUARE_DEFAULT_SIZE / 2 + objElement.resolvedStyle.width / 2;
			_highlighterElement.style.scale = new Scale(new Vector3(_initialScale, _initialScale, 1));

			_highlighterElement.SetDisplay(true);
			_highlighterElement.experimental.animation.Scale(_highlightedScale, GameConstants.Tutorial.TIME_HIGHLIGHT_FADE);
		}
	}
}