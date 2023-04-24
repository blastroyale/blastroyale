using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class WeaponDisplayView: IUIView
	{
		private const string UssMeleeWeapon = "weapon-display--melee";
		
		private VisualElement _root;
		private VisualElement _melee;
		private VisualElement _weapon;
		
		public void Attached(VisualElement element)
		{
			_root = element;
			_melee = element.Q("Melee").Required();
			_weapon = element.Q("Boomstick").Required();
			//throw new System.NotImplementedException();


		}

		public void SubscribeToEvents()
		{
			//throw new System.NotImplementedException();
		}

		public void UnsubscribeFromEvents()
		{
			//throw new System.NotImplementedException();
		}

		public void Switch()
		{
			_root.ToggleInClassList(UssMeleeWeapon);

			// TODO: Might want to do this with a delay
			if (_root.ClassListContains(UssMeleeWeapon))
			{
				_melee.BringToFront();
			}
			else
			{
				_weapon.BringToFront();
			}
		}
	}
}