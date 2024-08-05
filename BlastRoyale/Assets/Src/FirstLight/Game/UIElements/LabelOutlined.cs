using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class LabelOutlined : Label
	{
		private Label _internalLabel;

		public override VisualElement contentContainer => outlineHack ? _internalLabel : this;

		protected bool outlineHack { get; set; }

#if UNITY_EDITOR
		private IVisualElementScheduledItem _scheduledEditorUpdate;
#endif
		protected void Init()
		{
			if (_internalLabel != null)
			{
				hierarchy.Remove(_internalLabel);
			}

			if (outlineHack)
			{
				_internalLabel = new Label()
				{
					name = "Text",
					text = "Initial Value"
				};
				_internalLabel.AddToClassList("internal-label");
				_internalLabel.style.unityTextOutlineWidth = 0;
				_internalLabel.style.textShadow = new TextShadow()
				{
					color = Color.clear
				};
				hierarchy.Add(_internalLabel);

				this.RegisterValueChangedCallback(valueChange =>
				{
					if (valueChange.target != _internalLabel) return;
					
					_internalLabel.text = valueChange.newValue;
					if (resolvedStyle.textOverflow == TextOverflow.Ellipsis)
					{
						SetTextEllipticEnd(valueChange.newValue);
					}
				});
				Sync();

				_internalLabel.RegisterCallback<AttachToPanelEvent>((_) =>
				{
					Sync();
				});
				RegisterCallback<GeometryChangedEvent>((_) =>
				{
					Sync();
				});
#if UNITY_EDITOR
				_scheduledEditorUpdate?.Pause();
				_scheduledEditorUpdate = schedule.Execute(() =>
				{
					if (Application.isPlaying) return;
					Sync();
				}).Every(500);
#endif
			}
		}

		private string _lastResolvedEliptic;

		public void SetTextEllipticEnd(string param)
		{
			if (_lastResolvedEliptic == param) return;
			var index = param.Length;

			while (index > 3)
			{
				var ellipsed = param[..index];
				if (index != param.Length)
				{
					ellipsed += "...";
				}

				var textSize = MeasureTextSize(ellipsed,
					float.MaxValue, MeasureMode.AtMost, float.MaxValue, MeasureMode.AtMost);
				if (textSize.x == 0) return;
				if (textSize.x > contentRect.width)
				{
					if (index == param.Length)
					{
						index -= 3;
					}
					else
					{
						index--;
					}
				}
				else
				{
					_lastResolvedEliptic = text = ellipsed;
					return;
				}
			}

			text = text;
		}

		/// <summary>
		/// Do not call this method constructor, only used for unity internals!
		/// </summary>
		[Obsolete("Do not use default constructor", false)]
		public LabelOutlined()
		{
		}

		public LabelOutlined(string text, bool outlineHack = true)
		{
			this.outlineHack = outlineHack;
			this.text = text;
			Init();
		}

		private void Sync()
		{
			_internalLabel.text = text;
			_internalLabel.style.whiteSpace = resolvedStyle.whiteSpace;
			_internalLabel.style.textOverflow = resolvedStyle.textOverflow;
			_internalLabel.style.flexDirection = resolvedStyle.flexDirection;
			_internalLabel.style.alignItems = resolvedStyle.alignItems;
			_internalLabel.style.alignContent = resolvedStyle.alignContent;
			_internalLabel.style.justifyContent = resolvedStyle.justifyContent;
			if (_internalLabel.resolvedStyle.textOverflow == TextOverflow.Ellipsis)
			{
				SetTextEllipticEnd(text);
			}
		}

		public new class UxmlFactory : UxmlFactory<LabelOutlined, UxmlTraits>
		{
		}

		public new class UxmlTraits : TextElement.UxmlTraits
		{
			UxmlBoolAttributeDescription _outlineHack = new ()
			{
				name = "outline-hack",
				use = UxmlAttributeDescription.Use.Optional,
				defaultValue = true,
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var labelOutlined = (LabelOutlined) ve;
				labelOutlined.outlineHack = _outlineHack.GetValueFromBag(bag, cc);
				labelOutlined.Init();
			}
		}
	}
}