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
		private MatchStatusView _matchStatusView;

		// TODO: For testing only, remove
		private void Awake()
		{
			OnOpened();
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q("WeaponDisplay").Required().AttachView(this, out _weaponDisplayView);
			root.Q("KillFeed").Required().AttachView(this, out _killFeedView);
			root.Q("MatchStatus").Required().AttachView(this, out _matchStatusView);
		}

		[Button]
		public void DebugSpawnFeed()
		{
			_killFeedView.SpawnDeathNotification("GAMESTERWITHAREALLYLONGNAME", "CUPCAKE");
		}

		[Button]
		public void DebugStartCountdown(long warningTime = 5000, long shrinkingTime = 7000)
		{
			_matchStatusView.StartCountdown(warningTime, shrinkingTime);
		}
	}
}