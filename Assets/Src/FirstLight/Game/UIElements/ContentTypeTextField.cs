using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A TextField that allows setting the content / keyboard type.
	/// </summary>
	public class ContentTypeTextField : TextField
	{
		public TouchScreenKeyboardType keyboardType { get; set; }

		public ContentTypeTextField()
		{
			RegisterCallback<ClickEvent, ContentTypeTextField>(
				(_, tf) =>
				{
					TouchScreenKeyboard.Open(tf.text,
						keyboardType,
						true,
						tf.multiline,
						tf.isPasswordField);
				}, this);
		}

		public new class UxmlFactory : UxmlFactory<ContentTypeTextField, UxmlTraits>
		{
		}

		public new class UxmlTraits : TextField.UxmlTraits
		{
			private readonly UxmlEnumAttributeDescription<TouchScreenKeyboardType> _keyboardTypeAttribute = new()
			{
				name = "keyboard-type",
				defaultValue = TouchScreenKeyboardType.Default,
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var cttf = (ContentTypeTextField) ve;
				cttf.keyboardType = _keyboardTypeAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}