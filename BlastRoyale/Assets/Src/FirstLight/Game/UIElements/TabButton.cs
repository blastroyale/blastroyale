using System;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Copied from Shader Graph package
	/// </summary>
	public class TabButton : VisualElement
	{
		internal new class UxmlFactory : UxmlFactory<TabButton, UxmlTraits>
		{
		}

		internal new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription {name = "text"};

			private readonly UxmlStringAttributeDescription m_Target = new UxmlStringAttributeDescription
				{name = "target"};

			private readonly UxmlStringAttributeDescription _localizationKeyAttribute = new ()
			{
				name = "localization-key",
				use = UxmlAttributeDescription.Use.Required
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				TabButton item = ve as TabButton;

				item.m_Label.text = m_Text.GetValueFromBag(bag, cc);
				item.TargetId = m_Target.GetValueFromBag(bag, cc);
				item.LocalizeLabel(_localizationKeyAttribute.GetValueFromBag(bag, cc));
			}
		}

		static readonly string s_UssClassName = "unity-tab-button";
		static readonly string s_UssActiveClassName = s_UssClassName + "--active";

		private Label m_Label;

		public bool IsCloseable { get; set; }
		public string TargetId { get; private set; }
		public VisualElement Target { get; set; }
		private string localizationKey { get; set; }

		public event Action<TabButton> OnSelect;
		public event Action<TabButton> OnClose;

		public TabButton()
		{
			Init();
		}

		public TabButton(string text, VisualElement target)
		{
			Init();
			m_Label.text = text;
			Target = target;
		}

		public void LocalizeLabel(string labelKey)
		{
			localizationKey = labelKey;
			m_Label.text = LocalizationManager.TryGetTranslation(labelKey, out var translation)
				? translation
				: $"#{labelKey}#";
		}

		private void PopulateContextMenu(ContextualMenuPopulateEvent populateEvent)
		{
			DropdownMenu dropdownMenu = populateEvent.menu;

			if (IsCloseable)
			{
				dropdownMenu.AppendAction("Close Tab", e => OnClose(this));
			}
		}

		private void CreateContextMenu(VisualElement visualElement)
		{
			ContextualMenuManipulator menuManipulator = new ContextualMenuManipulator(PopulateContextMenu);

			visualElement.focusable = true;
			visualElement.pickingMode = PickingMode.Position;
			visualElement.AddManipulator(menuManipulator);

			visualElement.AddManipulator(menuManipulator);
		}

		private void Init()
		{
			AddToClassList(s_UssClassName);
			// styleSheets.Add(Resources.Load<StyleSheet>($"Styles/{styleName}"));
			//
			// VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>($"UXML/{UxmlName}");
			// visualTree.CloneTree(this);

			// m_Label = this.Q<Label>("Label");

			var topBar = new VisualElement();
			Add(topBar);
			topBar.AddToClassList("unity-tab-button__top-bar");

			var content = new VisualElement();
			Add(content);
			content.AddToClassList("unity-tab-button__content");

			m_Label = new LabelOutlined("Text") {name = "Label"};
			content.Add(m_Label);
			m_Label.AddToClassList("unity-tab-button__content-label");

			CreateContextMenu(this);

			RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
		}

		public void Select()
		{
			AddToClassList(s_UssActiveClassName);

			if (Target != null)
			{
				Target.style.display = DisplayStyle.Flex;
				Target.style.flexGrow = 1;
			}
		}

		public void Deselect()
		{
			RemoveFromClassList(s_UssActiveClassName);
			MarkDirtyRepaint();

			if (Target != null)
			{
				Target.style.display = DisplayStyle.None;
				Target.style.flexGrow = 0;
			}
		}

		private void OnMouseDownEvent(MouseDownEvent e)
		{
			switch (e.button)
			{
				case 0:
				{
					OnSelect?.Invoke(this);
					break;
				}

				case 2 when IsCloseable:
				{
					OnClose?.Invoke(this);
					break;
				}
			} // End of switch.

			e.StopImmediatePropagation();
		}
	}
}