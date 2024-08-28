using System;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that contains a outlined label 
	/// </summary>
	public class ButtonOutlined : LabelOutlined
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
		public ButtonOutlined()
		{
			AddToClassList("button-outlined");
			clickable = new Clickable((Action) null);
			focusable = true;
			tabIndex = 0;
		}

		public ButtonOutlined(string text, Action action) : base(text)
		{
			AddToClassList("button-outlined");
			clickable = new Clickable(action);
			focusable = true;
			tabIndex = 0;
		}

		public new class UxmlFactory : UxmlFactory<ButtonOutlined, UxmlTraits>
		{
		}
	}
}