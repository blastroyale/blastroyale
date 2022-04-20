using FirstLight.Game.Logic;
using FirstLight.UiService;
using FirstLight.Game.Utils;
using I2.Loc;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the DGame Countdown UI which displays when a player first joins a new or existing game.
	/// </summary>
	public class GameCountdownScreenPresenter : UiPresenter
	{
		[SerializeField, Required] protected Animation _animation;
		[SerializeField, Required] private AnimationClip _countdownAnimationClip;
		[SerializeField, Required] private AnimationClip _firstToXKillsCountdownClip;
		[SerializeField, Required] private TextMeshProUGUI _firstToXKillsText;

		private IGameDataProvider _gameDataProvider;

		protected void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void OnOpened()
		{
			var mapConfig = _gameDataProvider.AppDataProvider.CurrentMapConfig;

			_animation.clip = _firstToXKillsCountdownClip;
			_animation.Play();

			_firstToXKillsText.text =  string.Format(ScriptLocalization.AdventureMenu.FirstToXKills, mapConfig.GameEndTarget.ToString());
			
			this.LateCall(_animation.clip.length, Close);
		}
	}
}
