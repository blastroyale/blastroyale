using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class PlayerAvatarElement : VisualElement
	{
		private const string USS_BLOCK = "player-avatar";
		private const string USS_AVATAR = USS_BLOCK + "__avatar";
		private const string USS_MASK = USS_BLOCK + "__mask";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_FAME_LVL = USS_BLOCK + "__fame-lvl";
		private const string USS_FAME_STARS_HOLDER = USS_BLOCK + "__fame-stars-holder";
		private const string USS_FAME_STAR_1 = USS_BLOCK + "__fame-star-1";
		private const string USS_FAME_STAR_2 = USS_BLOCK + "__fame-star-2";
		private const string USS_FAME_STAR_3 = USS_BLOCK + "__fame-star-3";
		private const string USS_FAME_STAR_4 = USS_BLOCK + "__fame-star-4";
		private const string USS_FAME_STAR_5 = USS_BLOCK + "__fame-star-5";
		
		private readonly Label _fameLvl;
		private readonly VisualElement _starsHolder;
		
		public PlayerAvatarElement()
		{
			AddToClassList(USS_BLOCK);
			
			var avatar = new VisualElement {name = "avatar"};
			Add(avatar);
			avatar.AddToClassList(USS_AVATAR);
			{
				var mask = new VisualElement {name = "mask"};
				avatar.Add(mask);
				mask.AddToClassList(USS_MASK);
				{
					var pfp = new VisualElement {name = "pfp"};
					mask.Add(pfp);
					pfp.AddToClassList(USS_PFP);
				}
			}
			
			Add(_fameLvl = new Label("122") { name = "fame-lvl" });
			_fameLvl.AddToClassList(USS_FAME_LVL);
			
			Add(_starsHolder = new VisualElement { name = "fame-stars-holder" });
			_starsHolder.AddToClassList(USS_FAME_STARS_HOLDER);
			{
				var star1 = new VisualElement { name = "fame-star-1" };
				_starsHolder.Add(star1);
				star1.AddToClassList(USS_FAME_STAR_1);
				
				var star2 = new VisualElement { name = "fame-star-2" };
				_starsHolder.Add(star2);
				star2.AddToClassList(USS_FAME_STAR_2);
				
				var star3 = new VisualElement { name = "fame-star-3" };
				_starsHolder.Add(star3);
				star3.AddToClassList(USS_FAME_STAR_3);
				
				var star4 = new VisualElement { name = "fame-star-4" };
				_starsHolder.Add(star4);
				star4.AddToClassList(USS_FAME_STAR_4);
				
				var star5 = new VisualElement { name = "fame-star-5" };
				_starsHolder.Add(star5);
				star5.AddToClassList(USS_FAME_STAR_5);
			}
		}


		public new class UxmlFactory : UxmlFactory<PlayerAvatarElement>
		{
		}
	}
}