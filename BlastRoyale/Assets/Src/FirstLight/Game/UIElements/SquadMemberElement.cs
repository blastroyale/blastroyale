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
		private const int WOUNDED_PING_DURATION = 1000;
		private const int WOUNDED_PING_REPEAT = 2000;

		private const string USS_BLOCK = "squad-member";
		private const string USS_CONTAINER = USS_BLOCK + "__container";
		private const string USS_DEAD = USS_BLOCK + "--dead";
		private const string USS_KNOCKEDOUT = USS_BLOCK + "--knockedout";
		private const string USS_RING_EFFECT = USS_BLOCK + "__ring-effect";
		private const string USS_DEAD_CROSS = USS_BLOCK + "__dead-cross";
		private const string USS_BG = USS_BLOCK + "__bg";
		private const string USS_PFP_MASK = USS_BLOCK + "__pfp-mask";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_TEAM_COLOR = USS_BLOCK + "__team-color";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_SHIELD_HEALTH = USS_BLOCK + "__health-shield";

		private readonly VisualElement _bg;
		private readonly VisualElement _teamColor;
		private readonly VisualElement _pfpMask;
		private readonly VisualElement _pfp;
		private readonly Label _name;

		private readonly PlayerHealthShieldElement _healthShield;

		private PlayerRef _player;
		private int _pfpRequestHandle;

		private readonly ValueAnimation<float> _damageAnimation;
		private readonly IVisualElementScheduledItem _damageAnimationHandle;
		private readonly VisualElement _pingRingEffect;
		private ValueAnimation<float> _pingAnimation;

		public SquadMemberElement()
		{
			AddToClassList(USS_BLOCK);

			var container = new VisualElement {name = "container"};
			Add(container);
			container.AddToClassList(USS_CONTAINER);

			var deadCross = new VisualElement {name = "dead-cross"};
			Add(deadCross);
			deadCross.AddToClassList(USS_DEAD_CROSS);

			container.Add(_pingRingEffect = new VisualElement {name = "ring-effect"});
			_pingRingEffect.AddToClassList(USS_RING_EFFECT);

			container.Add(_bg = new VisualElement {name = "bg"});
			_bg.AddToClassList(USS_BG);

			container.Add(_teamColor = new VisualElement {name = "team-color"});
			_teamColor.AddToClassList(USS_TEAM_COLOR);
			{
				_teamColor.Add(_pfpMask = new VisualElement {name = "pfp-mask"});
				_pfpMask.AddToClassList(USS_PFP_MASK);
				{
					_pfpMask.Add(_pfp = new VisualElement {name = "pfp"});
					_pfp.AddToClassList(USS_PFP);
				}
			}

			container.Add(_name = new LabelOutlined(name) {text = "PLAYER NAME"});
			_name.AddToClassList(USS_NAME);

			container.Add(_healthShield = new PlayerHealthShieldElement {name = "health-shield"});
			_healthShield.AddToClassList(USS_SHIELD_HEALTH);

			_damageAnimation = _bg.experimental.animation.Start(1f, 0f, DAMAGE_ANIMATION_DURATION,
				(e, o) => e.style.opacity = o).KeepAlive();
			_damageAnimation.Stop();

			_pingAnimation = _pingRingEffect.experimental.animation.Scale(1f, WOUNDED_PING_DURATION)
				.KeepAlive();
			_pingAnimation.from = 0f;

			_damageAnimationHandle = _bg.schedule.Execute(_damageAnimation.Start);
			_damageAnimationHandle.Pause();

			this.Query().Build().ForEach(e => e.pickingMode = PickingMode.Ignore);
		}

		public void SetTeamColor(Color? color)
		{
			if (!color.HasValue)
				return;

			_teamColor.style.borderTopColor = color.Value;
			_teamColor.style.borderBottomColor = color.Value;
			_teamColor.style.borderLeftColor = color.Value;
			_teamColor.style.borderRightColor = color.Value;
		}

		public void SetPlayer(PlayerRef player, string playerName, Sprite pfpSprite, Color playerNameColor)
		{
			if (_player == player && pfpSprite == null) return;
			_player = player;

			_name.text = playerName;
			_name.style.color = playerNameColor;

			if (Application.isPlaying)
			{
				if (pfpSprite == null)
				{
					_pfp.style.backgroundImage = StyleKeyword.Null;
				}
				else
				{
					_pfp.style.backgroundImage = new StyleBackground(pfpSprite);
				}
			}
		}

		public void UpdateHealth(int previous, int current, int max)
		{
			_healthShield.UpdateHealth(previous, current, max);
		}

		public void UpdateShield(int previous, int current, int max)
		{
			_healthShield.UpdateShield(previous, current, max);
		}

		public void SetDead()
		{
			AddToClassList(USS_DEAD);
			SetKnocked(false);
		}

		public void SetKnocked(bool value)
		{
			var alreadyKnockedOut = ClassListContains(USS_KNOCKEDOUT);
			EnableInClassList(USS_KNOCKEDOUT, value);
			_healthShield.SetKnockedOut(value);
			if (value && !alreadyKnockedOut)
			{
				_pingRingEffect.schedule.Execute(() =>
				{
					_pingAnimation.Start();
				}).Every(WOUNDED_PING_REPEAT).Until(() => !ClassListContains(USS_KNOCKEDOUT));
			}
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