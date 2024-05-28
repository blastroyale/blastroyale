using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays a health bar (e.g. for destructibles).
	/// </summary>
	public class HealthStatusBarElement : VisualElement
	{
		public const int DAMAGE_ANIMATION_DURATION = 1500;

		private const string USS_BLOCK = "health-status-bar";
		private const string USS_BAR = USS_BLOCK + "__bar";

		private readonly VisualElement _bar;

		private readonly ValueAnimation<float> _opacityAnimation;
		private readonly IVisualElementScheduledItem _opacityAnimationHandle;

		public HealthStatusBarElement()
		{
			AddToClassList(USS_BLOCK);

			Add(_bar = new VisualElement {name = "bar"});
			_bar.AddToClassList(USS_BAR);

			_opacityAnimation = experimental.animation.Start(1f, 0f, DAMAGE_ANIMATION_DURATION,
				(e, o) => e.style.opacity = o).KeepAlive();
			_opacityAnimation.Stop();

			_opacityAnimationHandle = schedule.Execute(_opacityAnimation.Start);
			_opacityAnimationHandle.Pause();
		}

		/// <summary>
		/// Sets the health displayed in a 0-1 range (clamped).
		/// </summary>
		public void SetHealth(float health)
		{
			_bar.style.flexGrow = Mathf.Clamp01(health);

			_opacityAnimation.Stop();
			style.opacity = 1f;
			_opacityAnimationHandle.ExecuteLater(GameConstants.Visuals.GAMEPLAY_POST_ATTACK_HEALTHBAR_HIDE_DURATION);
		}

		public new class UxmlFactory : UxmlFactory<HealthStatusBarElement, UxmlTraits>
		{
		}
	}
}