using System;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace FirstLight.Game.UIElements.Kit
{
	public class KitButton : ImageButton
	{
		private static string PRIMARY = "-primary";
		private static string SECONDARY = "-secondary";
		private static string SUCCESS = "-success";
		private static string ERROR = "-error";
		private static string USS_BUTTON_CLASS = "flg-btn";
		private static string USS_BUTTON_TEXT_CLASS = USS_BUTTON_CLASS + "-text";
		private static string USS_BUTTON_ICON = USS_BUTTON_CLASS + "-icon";
		private static string USS_BUTTON_SHAPE_SQUARE = USS_BUTTON_CLASS + "-square";
		private static string USS_BUTTON_SHAPE_LONG = USS_BUTTON_CLASS + "-long";
		private static string USS_BUTTON_STYLE_SOLID = USS_BUTTON_CLASS + "-solid";
		private static string USS_BUTTON_STYLE_TRANSPARENT = USS_BUTTON_CLASS + "-transparent";
		private static string USS_BUTTON_COLOR_PRIMARY = USS_BUTTON_CLASS + PRIMARY;
		private static string USS_BUTTON_COLOR_SECONDARY = USS_BUTTON_CLASS + SECONDARY;
		private static string USS_BUTTON_COLOR_SUCCESS = USS_BUTTON_CLASS + SUCCESS;
		private static string USS_BUTTON_COLOR_ERROR = USS_BUTTON_CLASS + ERROR;
		private static string USS_BUTTON_DEFAULT_GAP = USS_BUTTON_CLASS + "-default-gap"; // This is a hack because unity doesn't support gap property

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

		public string BtnText
		{
			get => _text;
			set
			{
				_text = value;
				UpdateText();
			}
		}

		private VisualElement _iconElement;
		private Label _textElement;
		private ButtonStyle _btnStyle = ButtonStyle.Solid;
		private ButtonColor _btnColor = ButtonColor.Primary;
		private ButtonShape _btnShape = ButtonShape.Long;

		private string _btnIcon;
		private string _text;

		private void UpdateClasses()
		{
			ClearClassList();
			switch (_btnShape)
			{
				case ButtonShape.Square:
					AddToClassList(USS_BUTTON_SHAPE_SQUARE);
					break;
				case ButtonShape.Long:
					AddToClassList(USS_BUTTON_SHAPE_LONG);
					break;
				default:
					throw new NotSupportedException("Button format " + BtnShape.ToString() + " not supported yet!");
			}

			switch (_btnColor)
			{
				case ButtonColor.Primary:
					AddToClassList(USS_BUTTON_COLOR_PRIMARY);
					break;
				case ButtonColor.Secondary:
					AddToClassList(USS_BUTTON_COLOR_SECONDARY);
					break;
				case ButtonColor.Success:
					AddToClassList(USS_BUTTON_COLOR_SUCCESS);
					break;
				case ButtonColor.Error:
					AddToClassList(USS_BUTTON_COLOR_ERROR);
					break;
				default:
					throw new NotSupportedException("Button color " + BtnColor.ToString() + " not supported yet!");
			}

			switch (_btnStyle)
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

		private void UpdateText()
		{
			if (string.IsNullOrWhiteSpace(_text))
			{
				if (_textElement == null) return;
				Remove(_textElement);
				_textElement = null;
				return;
			}

			if (_textElement == null)
			{
				_textElement = new LabelOutlined(_text);
				_textElement.AddToClassList(USS_BUTTON_TEXT_CLASS);
			}
			else
			{
				_textElement.text = _text;
			}

			Add(_textElement);
		}

		public void AddDefaultGap()
		{
			this.AddToClassList(USS_BUTTON_DEFAULT_GAP);
		}

		public void Localize(string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return;
			if (!LocalizationManager.TryGetTranslation(key, out var translation))
			{
				translation = key;
				Debug.LogWarning($"Could not find translation for key {key} in element " + name);
			}

			BtnText = translation;
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

			private readonly UxmlStringAttributeDescription _buttonText = new ()
			{
				name = "btn-text",
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
				btn.BtnText = _buttonText.GetValueFromBag(bag, cc);
			}
		}
	}

	public enum ButtonShape
	{
		Square,
		Long,
		[Obsolete("Not supported yet")] Round,
	}

	public enum ButtonColor
	{
		Primary,
		Secondary,
		Success,
		Error
	}

	public enum ButtonStyle
	{
		Transparency,
		Solid,
	}
}