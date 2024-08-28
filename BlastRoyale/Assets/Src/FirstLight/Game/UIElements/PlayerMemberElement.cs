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
		private const string USS_LABEL = USS_BLOCK + "__playername-label";

		private readonly InGamePlayerAvatar _pfp;
		private Label _textLabel;

		public PlayerMemberElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_pfp = new InGamePlayerAvatar() {name = "ProfilePicture"});
			_pfp.AddToClassList(USS_PFP);

			Add(_textLabel = new LabelOutlined("PlayernameVeryLongYEAH") {name = "PlayerName"});
			_textLabel.AddToClassList(USS_LABEL);
		}

		public void SetData(string text, Color color)
		{
			_textLabel.text = text;
			_pfp.SetTeamColor(color);
		}

		public void SetPfpImage(Sprite image)
		{
			_pfp.SetSprite(image);
		}

		public new class UxmlFactory : UxmlFactory<PlayerMemberElement, UxmlTraits>
		{
		}
	}
}