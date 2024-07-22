using System;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public class LocalizedButton : LabelOutlined
	{
		private Clickable m_Clickable;

		public Clickable clickable
		{
			get => m_Clickable;
			set
			{
				if (m_Clickable != null && m_Clickable.target == this)
					this.RemoveManipulator(m_Clickable);
				m_Clickable = value;
				if (m_Clickable == null)
					return;
				this.AddManipulator(m_Clickable);
			}
		}

		public event Action clicked
		{
			add
			{
				if (m_Clickable == null)
					clickable = new Clickable(value);
				else
					m_Clickable.clicked += value;
			}
			remove
			{
				if (m_Clickable == null)
					return;
				m_Clickable.clicked -= value;
			}
		}

		[Obsolete("Do not use default constructor")]
		public LocalizedButton()
		{
			AddToClassList("localized-button");
			clickable = new Clickable((Action) null);
			focusable = true;
			tabIndex = 0;
		}

		public LocalizedButton(string elementName, Action action = null) : base(elementName)
		{
			AddToClassList("localized-button");
			clickable = new Clickable(action);
			focusable = true;
			tabIndex = 0;
		}

		public new class UxmlFactory : UxmlFactory<LocalizedButton, UxmlTraits>
		{
		}

		public new class UxmlTraits : LabelOutlined.UxmlTraits
		{
			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
			}
		}
	}
}