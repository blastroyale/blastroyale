using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UI.Elements
{
	[UxmlElement("FButton")]
	public partial class FButton : ImageButton
	{
		private static string USS = "fbutton";
		private static string USS_LABEL = "fbutton__label";
		private ThemeColors _color = ThemeColors.Primary;
		private string _text;

		private LabelOutlined _label;
		private VisualElement _topRightIconElement;

		[UxmlAttribute] public Sprite TopRightSprite { get; set; }

		[UxmlAttribute]
		public ThemeColors Color
		{
			get => _color;
			set
			{
				this.RemoveModifiers();
				AddToClassList(USS + "--" + value.ToString().ToLowerInvariant());
				_color = value;
			}
		}

		[UxmlAttribute]
		public string Text
		{
			get => _text;
			set
			{
				UpdateHierarchy();
				_text = value;
				_label.text = value;
			}
		}

		public FButton()
		{
			AddToClassList(USS);

			_topRightIconElement = new VisualElement() {name = "TopRightIcon"};
			_topRightIconElement.AddToClassList(USS + "__top-right-icon");
			Add(_topRightIconElement);
			
			var dotHolder = new VisualElement() {name = "Dot-Holder",}.AddClass(USS + "__dots-holder");
			dotHolder.Add(new VisualElement() {name = "Dot-Left"}.AddClass(USS + "__dots-holder__left"));
			dotHolder.Add(new VisualElement() {name = "Dot-Right"}.AddClass(USS + "__dots-holder__right"));
			Add(dotHolder);
		}

		public void UpdateHierarchy()
		{
			if (_label == null)
			{
				_label = new LabelOutlined("").AddClass(USS_LABEL);
				Add(_label);
			}
		}
	}
}