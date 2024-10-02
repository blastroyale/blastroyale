using System;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace FirstLight.Game.UIElements.Kit
{
	public class KitButton : ImageButton
	{
		private static string USS_BUTTON_CLASS = "flg-btn";
		private static string USS_BUTTON_ICON = "flg-btn-icon";
		private static string USS_BUTTON_SHAPE_SQUARE = USS_BUTTON_CLASS + "-square";
		private static string USS_BUTTON_STYLE_SOLID = USS_BUTTON_CLASS + "-solid";
		private static string USS_BUTTON_STYLE_TRANSPARENT = USS_BUTTON_CLASS + "-transparent";
		private static string USS_BUTTON_COLOR_PRIMARY = USS_BUTTON_CLASS + "-primary";

		public ButtonStyle BtnStyle
		{
			get => _btnStyle;
			set
			{
				_btnStyle = value;
				UpdateClasses();
			}
		}

		public ButtonColor BtnColor
		{
			get => _btnColor;
			set
			{
				_btnColor = value;
				UpdateClasses();
			}
		}

		public ButtonShape BtnShape
		{
			get => _btnShape;
			set
			{
				_btnShape = value;
				UpdateClasses();
			}
		}

		/// <see cref="FirstLight.Game.UIElements.Kit"/>
		public string BtnIcon
		{
			get => _btnIcon;
			set
			{
				_btnIcon = value;
				UpdateClasses();
			}
		}

		private VisualElement _iconElement;
		private ButtonStyle _btnStyle;
		private ButtonColor _btnColor;
		private ButtonShape _btnShape;

		private string _btnIcon;

		private void UpdateClasses()
		{
			ClearClassList();
			switch (BtnShape)
			{
				case ButtonShape.Square:
					AddToClassList(USS_BUTTON_SHAPE_SQUARE);
					break;
				default:
					throw new NotSupportedException("Button format " + BtnShape.ToString() + " not supported yet!");
			}

			switch (BtnColor)
			{
				case ButtonColor.Primary:
					AddToClassList(USS_BUTTON_COLOR_PRIMARY);
					break;
				default:
					throw new NotSupportedException("Button color " + BtnColor.ToString() + " not supported yet!");
			}

			switch (BtnStyle)
			{
				case ButtonStyle.Solid:
					AddToClassList(USS_BUTTON_STYLE_SOLID);
					break;
				case ButtonStyle.Transparency:
					AddToClassList(USS_BUTTON_STYLE_TRANSPARENT);
					break;
				default:
					throw new NotSupportedException("Button style " + BtnStyle.ToString() + " not supported yet!");
			}

			if (_iconElement != null)
			{
				Remove(_iconElement);
				_iconElement = null;
			}

			if (string.IsNullOrWhiteSpace(BtnIcon))
			{
				return;
			}

			_iconElement = new VisualElement() {name = "Icon"}.AddClass(USS_BUTTON_ICON);
			_iconElement.AddToClassList("icon--" + BtnIcon);
			Add(_iconElement);
		}

		public new class UxmlFactory : UxmlFactory<KitButton, UxmlTraits>
		{
		}

		public new class UxmlTraits : ImageButton.UxmlTraits
		{
			private readonly UxmlEnumAttributeDescription<ButtonShape> _buttonShape = new ()
			{
				name = "btn-shape",
				defaultValue = ButtonShape.Square
			};

			private readonly UxmlEnumAttributeDescription<ButtonColor> _buttonColor = new ()
			{
				name = "btn-color",
				defaultValue = ButtonColor.Primary
			};

			private readonly UxmlEnumAttributeDescription<ButtonStyle> _buttonStyle = new ()
			{
				name = "btn-style",
				defaultValue = ButtonStyle.Solid
			};

			private readonly UxmlStringAttributeDescription _buttonIcon = new ()
			{
				name = "btn-icon",
				defaultValue = ""
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var btn = (KitButton) ve;
				btn.BtnStyle = _buttonStyle.GetValueFromBag(bag, cc);
				btn.BtnColor = _buttonColor.GetValueFromBag(bag, cc);
				btn.BtnShape = _buttonShape.GetValueFromBag(bag, cc);
				btn.BtnIcon = _buttonIcon.GetValueFromBag(bag, cc);
			}
		}
	}

	public enum ButtonShape
	{
		Square,
		[Obsolete("Not supported yet")] Long,
		[Obsolete("Not supported yet")] Round,
	}

	public enum ButtonColor
	{
		Primary
	}

	public enum ButtonStyle
	{
		Transparency,
		Solid,
	}
}