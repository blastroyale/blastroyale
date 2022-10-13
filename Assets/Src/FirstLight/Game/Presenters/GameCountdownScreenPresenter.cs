using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.UiService;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
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

		private IGameServices _services;

		protected void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void OnOpened()
		{
			var f = QuantumRunner.Default.Game.Frames.Verified;
			var gameModeConfig = f.Context.GameModeConfig;

			_animation.clip = _firstToXKillsCountdownClip;
			_animation.Play();

			_firstToXKillsText.text = string.Format(ScriptLocalization.AdventureMenu.FirstToXKills,
			                                        gameModeConfig.CompletionKillCount);

			_services.MessageBrokerService.Publish(new MatchCountdownStartedMessage());
			
			this.LateCoroutineCall(_animation.clip.length, () => Close(true));
		}
	}
}