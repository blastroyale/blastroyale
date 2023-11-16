using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	public class SquadMemberElement : VisualElement
	{
		private const int DAMAGE_ANIMATION_DURATION = 500;

		private const string USS_BLOCK = "squad-member";
		private const string USS_CONTAINER = USS_BLOCK + "__container";
		private const string USS_DEAD = USS_BLOCK + "--dead";
		private const string USS_DEAD_CROSS = USS_BLOCK + "__dead-cross";
		private const string USS_BG = USS_BLOCK + "__bg";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_TEAM_COLOR = USS_BLOCK + "__team-color";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_SHIELD_HEALTH = USS_BLOCK + "__health-shield";

		private readonly VisualElement _bg;
		private readonly VisualElement _pfp;
		private readonly VisualElement _teamColor;
		private readonly Label _name;

		private readonly PlayerHealthShieldElement _healthShield;

		private PlayerRef _player;
		private int _pfpRequestHandle;
		private bool _showRealDamage;

		private readonly ValueAnimation<float> _damageAnimation;
		private readonly IVisualElementScheduledItem _damageAnimationHandle;

		public ref bool ShowRealDamage => ref _showRealDamage;

		public SquadMemberElement()
		{
			AddToClassList(USS_BLOCK);

			var container = new VisualElement {name = "container"};
			Add(container);
			container.AddToClassList(USS_CONTAINER);

			var deadCross = new VisualElement {name = "dead-cross"};
			Add(deadCross);
			deadCross.AddToClassList(USS_DEAD_CROSS);

			container.Add(_bg = new VisualElement {name = "bg"});
			_bg.AddToClassList(USS_BG);

			container.Add(_teamColor = new VisualElement {name = "team-color"});
			_teamColor.AddToClassList(USS_TEAM_COLOR);
			{
				_teamColor.Add(_pfp = new VisualElement {name = "pfp"});
				_pfp.AddToClassList(USS_PFP);
			}

			container.Add(_name = new Label("PLAYER NAME") {name = "name"});
			_name.AddToClassList(USS_NAME);

			container.Add(_healthShield = new PlayerHealthShieldElement {name = "health-shield"});
			_healthShield.AddToClassList(USS_SHIELD_HEALTH);

			_damageAnimation = _bg.experimental.animation.Start(1f, 0f, DAMAGE_ANIMATION_DURATION,
				(e, o) => e.style.opacity = o).KeepAlive();
			_damageAnimation.Stop();

			_damageAnimationHandle = _bg.schedule.Execute(_damageAnimation.Start);
			_damageAnimationHandle.Pause();

			this.Query().Build().ForEach(e => e.pickingMode = PickingMode.Ignore);
		}

		public void SetTeamColor(Color? color)
		{
			if (!color.HasValue) _teamColor.SetDisplay(false);
			else _teamColor.style.backgroundColor = color.Value;
		}

		public void SetPlayer(PlayerRef player, string playerName, int level, string pfpUrl, Color playerNameColor)
		{
			if (_player == player) return;
			_player = player;

			_name.text = playerName;
			_name.style.color = playerNameColor;

			if (Application.isPlaying)
			{
				// pfpUrl =
				// 	$"https://mainnetprodflghubstorage.blob.core.windows.net/collections/corpos/{Random.Range(1, 888)}.png";

				if (!string.IsNullOrEmpty(pfpUrl))
				{
					var textureService = MainInstaller.Resolve<IGameServices>().RemoteTextureService;
					textureService.CancelRequest(_pfpRequestHandle);
					_pfpRequestHandle = MainInstaller.Resolve<IGameServices>().RemoteTextureService.RequestTexture(
						pfpUrl,
						tex =>
						{
							if (_pfp != null && _pfp.panel != null)
							{
								_pfp.style.backgroundImage = new StyleBackground(tex);
							}
						}, null);
				}
				else
				{
					_pfp.style.backgroundImage = StyleKeyword.Null;
				}
			}
		}

		public void UpdateHealth(int previous, int current, int max)
		{
			_healthShield.UpdateHealth(previous, current, max, !_showRealDamage);
		}

		public void UpdateShield(int previous, int current, int max)
		{
			_healthShield.UpdateShield(previous, current, max, !_showRealDamage);
		}

		public void SetDead()
		{
			AddToClassList(USS_DEAD);
		}

		public void PingDamage()
		{
			_damageAnimation.Stop();
			_bg.style.opacity = 1;
			_damageAnimationHandle.ExecuteLater(1000);
		}

		public new class UxmlFactory : UxmlFactory<SquadMemberElement, UxmlTraits>
		{
		}
	}
}