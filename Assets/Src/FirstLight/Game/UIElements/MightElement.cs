using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the might graphic.
	/// </summary>
	public class MightElement : VisualElement
	{
		private const string UssBlock = "might-display";
		private const string UssLabel = UssBlock + "__label";
		private const string UssIcon = UssBlock + "__icon";

		private Label _label;

		private float _currentMight = 0;

		public MightElement()
		{
			AddToClassList(UssBlock);

			Add(_label = new Label("MIGHT 123") {name = "label"});
			_label.AddToClassList(UssLabel);

			var icon = new VisualElement();
			icon.AddToClassList(UssIcon);
			Add(icon);
		}

		public void SetMight(float value, bool animate)
		{
			if (animate)
			{
				_label.experimental.animation.Start(_currentMight, value, 300, (l, val) =>
					SetValue((Label) l, val)
				);
			}
			else
			{
				SetValue(_label, value);
			}
			
			_currentMight = value;
		}

		private static void SetValue(Label label, float value)
		{
			label.text = string.Format(ScriptLocalization.UITEquipment.might, value.ToString("F0"));
		}

		public new class UxmlFactory : UxmlFactory<MightElement>
		{
		}
	}
}