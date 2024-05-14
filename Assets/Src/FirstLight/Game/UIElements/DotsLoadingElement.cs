using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the might graphic.
	/// </summary>
	public class DotsLoadingElement : VisualElement
	{
		public int DotSize
		{
			get => _dotSize;
			set => _dotSize = value;
		}

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

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlIntAttributeDescription _sizeAttribute = new ()
			{
				name = "dot-size",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = 10
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				((DotsLoadingElement) ve).DotSize = _sizeAttribute.GetValueFromBag(bag, cc);
				((DotsLoadingElement) ve).Start();
			}
		}
	}
}