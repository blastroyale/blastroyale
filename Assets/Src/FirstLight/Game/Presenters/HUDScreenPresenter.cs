using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HUDScreenPresenter : UiToolkitPresenterData<HUDScreenPresenter.StateData>
	{
		public struct StateData
		{
		}

		private WeaponDisplayView _weaponDisplayView;
		private KillFeedView _killFeedView;

		// TODO: For testing only, remove
		private void Awake()
		{
			OnOpened();
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q("WeaponDisplay").Required().AttachView(this, out _weaponDisplayView);
			root.Q("KillFeed").Required().AttachView(this, out _killFeedView);
		}

		[Button]
		public void DebugSpawnFeed()
		{
			_killFeedView.SpawnDeathNotification("GAMESTERWITHAREALLYLONGNAME", "CUPCAKE");
		}
	}
}