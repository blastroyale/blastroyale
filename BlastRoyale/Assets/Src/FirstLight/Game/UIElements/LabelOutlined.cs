using FirstLight.Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public sealed class LabelOutlined : Label
	{
		private Label _internalLabel;

		private string textContent;

		public LabelOutlined()
		{
			_internalLabel = new Label()
			{
				name = "Text",
				text = text
			};
			_internalLabel.AddToClassList("fill-parent");
			_internalLabel.style.SetPadding(0);
			_internalLabel.style.SetMargin(0);
			_internalLabel.style.unityTextOutlineWidth = 0;
			_internalLabel.style.textShadow = new TextShadow()
			{
				color = Color.clear
			};
			Add(_internalLabel);
			Sync();
			RegisterCallback<GeometryChangedEvent>((ev) =>
			{
				Sync();
			});
#if UNITY_EDITOR

			schedule.Execute(() =>
			{
				if (Application.isPlaying) return;
				Sync();
			}).Every(500);
#endif
		}

		private void Sync()
		{
			_internalLabel.text = text;
			_internalLabel.style.whiteSpace = resolvedStyle.whiteSpace;
		}

		public new class UxmlFactory : UxmlFactory<LabelOutlined, UxmlTraits>
		{
		}
	}
}