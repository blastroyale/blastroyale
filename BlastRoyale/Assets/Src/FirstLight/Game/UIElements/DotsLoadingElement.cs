using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the might graphic.
	/// </summary>
	public class DotsLoadingElement : VisualElement
	{
		private static readonly CustomStyleProperty<Color> S_Color = new ("--color");
		private static readonly CustomStyleProperty<Color> S_SecondaryColor = new ("--secondary-color");
		private static readonly CustomStyleProperty<int> S_DotSize = new ("--dot-size");

		private VisualElement[] _dots;
		private Color _color = new Color(0.04f, 0.21f, 0.6f);
		private Color _filledColor = new Color(0.92f, 0.98f, 1f);
		private int _amount = 3;
		private int _dotSize = 12;
		private int _speed = 666;
		private int _currentIndex;
		private IVisualElementScheduledItem _animationScheduler;

		public DotsLoadingElement()
		{
			RegisterCallback<CustomStyleResolvedEvent>(OnResolvedStyle);
			Start();
		}

		private void OnResolvedStyle(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(S_Color, out var primaryColor))
			{
				_color = primaryColor;
			}

			if (evt.customStyle.TryGetValue(S_SecondaryColor, out var secondaryColor))
			{
				_filledColor = secondaryColor;
			}

			if (evt.customStyle.TryGetValue(S_DotSize, out var dotSize))
			{
				_dotSize = dotSize;
			}

			Start();
		}

		public void Start()
		{
			if (_dots != null)
			{
				foreach (var visualElement in _dots)
				{
					Remove(visualElement);
				}
			}

			_animationScheduler?.Pause();

			style.flexDirection = FlexDirection.Row;
			style.paddingRight = 4;
			style.paddingBottom = 4;
			style.paddingTop = 4;
			style.paddingLeft = 4;
			_dots = new VisualElement[_amount];
			for (int x = 0; x < _amount; x++)
			{
				var dot = new VisualElement();
				dot.style.backgroundColor = _color;
				dot.style.borderBottomRightRadius = 50;
				dot.style.borderBottomLeftRadius = 50;
				dot.style.borderTopLeftRadius = 50;
				dot.style.borderTopRightRadius = 50;
				dot.style.height = _dotSize;
				dot.style.width = _dotSize;
				dot.style.marginRight = _dotSize / 2;
				dot.style.marginRight = _dotSize / 2;

				Add(dot);
				_dots[x] = dot;
			}

			_animationScheduler = schedule.Execute(() =>
			{
				for (int i = 0; i < _amount; i++)
				{
					_dots[i].style.backgroundColor = _currentIndex == i ? _filledColor : _color;
				}

				_currentIndex++;
				if (_currentIndex > _amount - 1)
				{
					_currentIndex = 0;
				}
			}).Every(_speed).StartingIn(_speed);
		}

		public new class UxmlFactory : UxmlFactory<DotsLoadingElement, UxmlTraits>
		{
		}
	}
}