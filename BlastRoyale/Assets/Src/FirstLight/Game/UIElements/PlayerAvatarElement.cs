using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.Assertions;
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
		private const string USS_AVATAR_NFT = USS_BLOCK + "--nft";

		private const string USS_STARS_BRONZE = USS_FAME_STARS_HOLDER + "--bronze";
		private const string USS_STARS_SILVER = USS_FAME_STARS_HOLDER + "--silver";
		private const string USS_STARS_GOLD = USS_FAME_STARS_HOLDER + "--gold";
		private const string USS_STARS_DIAMOND = USS_FAME_STARS_HOLDER + "--diamond";

		private readonly Label _fameLvl;
		private readonly VisualElement _starsHolder;
		private readonly VisualElement _star1;
		private readonly VisualElement _star2;
		private readonly VisualElement _star3;
		private readonly VisualElement _star4;
		private readonly VisualElement _star5;
		private readonly VisualElement _pfp;
		private readonly RemoteAvatarElement _avatarHolder;

		public PlayerAvatarElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_avatarHolder = new RemoteAvatarElement() {name = "avatar"});
			_avatarHolder.AddToClassList(USS_AVATAR);

			Add(_fameLvl = new Label("") {name = "fame-lvl"});
			_fameLvl.AddToClassList(USS_FAME_LVL);

			Add(_starsHolder = new VisualElement {name = "fame-stars-holder"});
			_starsHolder.AddToClassList(USS_FAME_STARS_HOLDER);
			_starsHolder.AddToClassList(USS_STARS_BRONZE);
			{
				_starsHolder.Add(_star1 = new VisualElement {name = "fame-star-1"});
				_star1.AddToClassList(USS_FAME_STAR_1);

				_starsHolder.Add(_star2 = new VisualElement {name = "fame-star-2"});
				_star2.AddToClassList(USS_FAME_STAR_2);

				_starsHolder.Add(_star3 = new VisualElement {name = "fame-star-3"});
				_star3.AddToClassList(USS_FAME_STAR_3);

				_starsHolder.Add(_star4 = new VisualElement {name = "fame-star-4"});
				_star4.AddToClassList(USS_FAME_STAR_4);

				_starsHolder.Add(_star5 = new VisualElement {name = "fame-star-5"});
				_star5.AddToClassList(USS_FAME_STAR_5);
			}
		}

		public void SetDisplayLevel(bool display)
		{
			_fameLvl.SetDisplay(display);
		}

		public void SetLevel(uint level)
		{
			_fameLvl.text = level.ToString();

			// We use current stars (bronze to diamond) up to Fame Level 100 which is about 2 years of playing casually
			var visibleStars = level < 100 ? Mathf.FloorToInt((((level - 1) % 25) / 5f) + 1) : 5;
			var colorLevel = level < 100 ? Mathf.FloorToInt((level - 1) / 25f) : 3;

			SetVisibleStars((uint) visibleStars);
			SetStarsColorLevel((uint) colorLevel);
		}

		public void LoadFromTask(UniTask<Sprite> loadSpriteTask)
		{
			_avatarHolder.SetAvatar(loadSpriteTask.ContinueWith(sprite => sprite.texture)).Forget();
		}

		public void SetAvatar(string url, CancellationToken  cancellationToken)
		{
			_avatarHolder.SetAvatar(url, cancellationToken).Forget();
		}

		public void SetVisibleStars(uint visibleStars)
		{
			Assert.IsTrue(visibleStars is <= 5 and >= 0, "Can only show 0 - 5 visible stars");

			_star1.SetDisplay(visibleStars is 4 or 5);
			_star2.SetDisplay(visibleStars is 2 or 3 or 4 or 5);
			_star3.SetDisplay(visibleStars is 1 or 3 or 5);
			_star4.SetDisplay(visibleStars is 2 or 3 or 4 or 5);
			_star5.SetDisplay(visibleStars is 4 or 5);
		}

		private void SetStarsColorLevel(uint colorLevel)
		{
			_starsHolder.RemoveModifiers();

			switch (colorLevel)
			{
				case 0:
					_starsHolder.AddToClassList(USS_STARS_BRONZE);
					break;
				case 1:
					_starsHolder.AddToClassList(USS_STARS_SILVER);
					break;
				case 2:
					_starsHolder.AddToClassList(USS_STARS_GOLD);
					break;
				case >= 3:
					_starsHolder.AddToClassList(USS_STARS_DIAMOND);
					break;
			}
		}

		public new class UxmlFactory : UxmlFactory<PlayerAvatarElement>
		{
		}
	}
}