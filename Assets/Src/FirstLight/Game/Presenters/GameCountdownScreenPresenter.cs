using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.UiService;
using FirstLight.Game.Signals;
using FirstLight.Game.Utils;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the DGame Countdown UI which displays when a player first joins a new or existing game.
	/// </summary>
	public class GameCountdownScreenPresenter : UiPresenter
	{
		[SerializeField] protected Animation _animation;

		[SerializeField] private AnimationClip _countdownAnimationClip;
		[SerializeField] private AnimationClip _firstToXKillsCountdownClip;
		[SerializeField] private TextMeshProUGUI _firstToXKillsText;

		private IGameServices _services;

		protected void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void OnOpened()
		{
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;

			_animation.clip = _firstToXKillsCountdownClip;
			_animation.Play();

			_firstToXKillsText.text =  string.Format(ScriptLocalization.AdventureMenu.FirstToXKills, mapConfig.GameEndTarget.ToString());
			
			this.LateCoroutineCall(_animation.clip.length, Close);
		}
	}
}
