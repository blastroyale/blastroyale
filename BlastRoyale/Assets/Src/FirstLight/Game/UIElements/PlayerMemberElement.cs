using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays player avatar element with border, mask and background image.
	/// </summary>
	public class PlayerMemberElement : VisualElement
	{
		private const string USS_BLOCK = "squad-member";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_TEAM_COLOR = USS_BLOCK + "__team-color";
		private const string USS_PFP_MASK = USS_BLOCK + "__pfp-mask";
		private const string USS_LABEL = USS_BLOCK + "__playername-label";
		
		private readonly VisualElement _pfpMask;
		private readonly VisualElement _pfp;
		private Label _textLabel;
		
		public PlayerMemberElement()
		{
			AddToClassList(USS_BLOCK);
			
			Add(_pfpMask = new VisualElement {name = "pfp-mask"});
			_pfpMask.AddToClassList(USS_PFP_MASK);
			_pfpMask.Add(_pfp = new VisualElement {name = "pfp"});
			
			_pfp.AddToClassList(USS_PFP);

			Add(_textLabel = new Label("text"));
			_textLabel.AddToClassList(USS_LABEL);
		}

		public void SetData(string text, Color color)
		{
			_textLabel.text = text;
			SetTeamColor(color);
			
		}

		public void SetTeamColor(Color color)
		{
			_pfpMask.style.borderBottomColor = color;
			_pfpMask.style.borderTopColor = color;
			_pfpMask.style.borderLeftColor = color;
			_pfpMask.style.borderRightColor = color;
		}

		public void SetPfpImage(Sprite image)
		{
			_pfp.style.backgroundImage = new StyleBackground(image.texture);
		}

		public new class UxmlFactory : UxmlFactory<PlayerMemberElement, UxmlTraits>
		{
		}
	}
}