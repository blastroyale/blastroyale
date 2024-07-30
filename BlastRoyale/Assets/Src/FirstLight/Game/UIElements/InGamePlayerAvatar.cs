using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class InGamePlayerAvatar : VisualElement
	{
		private readonly VisualElement _pfp;
		private const string USS_BLOCK = "in-game-avatar";
		private const string USS_THIN_MODIFIER = USS_BLOCK + "--thin";
		private const string USS_NO_BORDER = USS_BLOCK + "--no-border";
		private const string USS_PFP_MASK = USS_BLOCK + "__pfp-mask";
		private const string USS_PFP = USS_BLOCK + "__pfp";

		public InGamePlayerAvatar()
		{
			AddToClassList(USS_BLOCK);
			{
				var mask = new VisualElement {name = "pfp-mask"};
				Add(mask);
				mask.AddToClassList(USS_PFP_MASK);
				{
					mask.Add(_pfp = new VisualElement {name = "pfp"});
					_pfp.AddToClassList(USS_PFP);
				}
			}
		}

		public InGamePlayerAvatar SetTeamColor(Color? color)
		{
			if (!color.HasValue)
				return this;

			RemoveFromClassList(USS_NO_BORDER);
			style.borderTopColor = color.Value;
			style.borderBottomColor = color.Value;
			style.borderLeftColor = color.Value;
			style.borderRightColor = color.Value;
			return this;
		}

		public void RemoveBorder()
		{
			AddToClassList(USS_NO_BORDER);
		}

		public InGamePlayerAvatar SetSprite(Sprite sprite)
		{
			if (sprite == null)
			{
				_pfp.style.backgroundImage = StyleKeyword.Null;
			}
			else
			{
				_pfp.style.backgroundImage = new StyleBackground(sprite);
			}

			return this;
		}

		public InGamePlayerAvatar SetThin()
		{
			AddToClassList(USS_THIN_MODIFIER);
			return this;
		}

		public new class UxmlFactory : UxmlFactory<InGamePlayerAvatar>
		{
		}
	}
}