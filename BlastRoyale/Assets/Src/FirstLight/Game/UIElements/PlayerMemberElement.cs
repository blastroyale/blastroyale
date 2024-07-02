using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	public class PlayerMemberElement : VisualElement
	{
		private const string USS_BLOCK = "squad-member";
		private const string USS_CONTAINER = USS_BLOCK + "__container";

		private const string USS_PFP = USS_BLOCK + "__pfp1";
		private const string USS_TEAM_COLOR = USS_BLOCK + "__team-color1";
		private const string USS_PFP_MASK = USS_BLOCK + "__pfp-mask1";
		
		private readonly VisualElement _teamColor;
		private readonly VisualElement _pfpMask;
		private readonly VisualElement _pfp;
		
		public PlayerMemberElement()
		{
			AddToClassList(USS_TEAM_COLOR);

			//var container = new VisualElement {name = "container"};
			//Add(container);
			//container.AddToClassList(USS_CONTAINER);
			
			Add(_pfpMask = new VisualElement {name = "pfp-mask"});
			_pfpMask.AddToClassList(USS_PFP_MASK);
			{
				_pfpMask.Add(_pfp = new VisualElement {name = "pfp"});
				_pfp.AddToClassList(USS_PFP);
			}

			//_teamColor.AddToClassList(USS_TEAM_COLOR);
			//{
			//	_teamColor.Add(_pfpMask = new VisualElement {name = "pfp-mask"});
			//	_pfpMask.AddToClassList(USS_PFP_MASK);
				
			//	_pfpMask.Add(_pfp = new VisualElement {name = "pfp"});
			//	_pfp.AddToClassList(USS_PFP);
			//}
			
			this.Query().Build().ForEach(e => e.pickingMode = PickingMode.Ignore);
		}

		public void SetTeamColor(Color? color)
		{
			if (!color.HasValue) _teamColor.SetDisplay(false);
			else _teamColor.style.backgroundColor = color.Value;
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