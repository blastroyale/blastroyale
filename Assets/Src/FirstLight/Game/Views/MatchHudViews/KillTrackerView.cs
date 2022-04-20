using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View handles the Kill Tracker View in the UI:
	/// - Shows the avatar and name of a player who killed another player. 
	/// </summary>
	public class KillTrackerView : MonoBehaviour, IPoolEntityObject<KillTrackerView>
	{
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _animationClipFadeIn;
		[SerializeField, Required] private AnimationClip _animationClipFadeOut;
		[SerializeField, Required] private TextMeshProUGUI _killerName;
		[SerializeField, Required] private TextMeshProUGUI _killedName;
		[SerializeField, Required] private TextMeshProUGUI _suicideName;
		[SerializeField, Required] private GameObject _killerNameHolder;
		[SerializeField, Required] private GameObject _killedNameHolder;
		[SerializeField, Required] private GameObject _suicideNameHolder;
		[SerializeField, Required] private Image _skullImage;
		
		private IGameServices _services;
		private IObjectPool<KillTrackerView> _pool;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
		}

		/// <summary>
		/// Spawns slot view to show the player data.
		/// </summary>
		public void SetInfo(string killer, GameId killerSkin, string deadPlayer, GameId deadSkin, bool suicide)
		{
			_killerName.text =  killer;
			_killedName.text = deadPlayer;
			_suicideName.text = "";

			_killerName.color = Color.white;

			_killerNameHolder.SetActive(!suicide);
			_killedNameHolder.SetActive(!suicide);
			_skullImage.gameObject.SetActive(!suicide);

			_suicideNameHolder.SetActive(suicide);
			
			_animation.clip = _animationClipFadeIn;
			_animation.Rewind();
			_animation.Play();

			if (suicide)
			{
				_suicideName.text = string.Format(ScriptLocalization.AdventureMenu.Suicide, killer);
				_killerName.text = "";
				_killedName.text = "";
			}

			this.LateCall(_animation.clip.length, PlayFadeOutAnimation);
		}
		
		private void PlayFadeOutAnimation()
		{
			_animation.clip = _animationClipFadeOut;
			_animation.Rewind();
			_animation.Play();
			
			this.LateCall(_animation.clip.length, ()=> Despawn());
		}

		/// <inheritdoc />
		public void Init(IObjectPool<KillTrackerView> pool)
		{
			_pool = pool;
		}

		/// <inheritdoc />
		public bool Despawn()
		{
			return _pool.Despawn(this);
		}
	}
}
