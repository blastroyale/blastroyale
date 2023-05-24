using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Presenters;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.TestCases.Helpers
{
	public class EquipmentUIHelper : TestHelper
	{
		private UIHelper _uiHelper;


		private Dictionary<GameIdGroup, string> _groupToSlotName = new()
		{
			{ GameIdGroup.Weapon, "WeaponCategory" },
			{ GameIdGroup.Shield, "ShieldCategory" },
			{ GameIdGroup.Helmet, "HelmetCategory" },
			{ GameIdGroup.Amulet, "AmuletCategory" },
			{ GameIdGroup.Armor, "ArmorCategory" },
		};

		public EquipmentUIHelper(FLGTestRunner testRunner, UIHelper uiHelper) : base(testRunner)
		{
			_uiHelper = uiHelper;
		}


		public IEnumerator OpenEquipmentSlot(GameIdGroup group)
		{
			yield return _uiHelper.WaitForPresenter<EquipmentPresenter>();

			yield return _uiHelper.TouchOnElementByName(_groupToSlotName[group]);
			yield return new WaitForSeconds(1f);
		}

		public IEnumerator SelectEquipmentAtSelectionScreen(UniqueId id)
		{
			yield return _uiHelper.WaitForPresenter<EquipmentSelectionPresenter>();

			var presenter = _uiHelper.GetPresenter<EquipmentSelectionPresenter>();
			var card = presenter.GetCardFromItemId(id);
			yield return _uiHelper.TouchOnElement(presenter.Document.rootVisualElement, card);
			yield return new WaitForSeconds(1f);
		}

		public IEnumerator ClickEquipButton()
		{
			yield return _uiHelper.TouchOnElementByName("EquipButton");
			yield break;
		}
	}
}