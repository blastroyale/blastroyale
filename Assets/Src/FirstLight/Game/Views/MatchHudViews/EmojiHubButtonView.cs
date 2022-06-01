using System.Threading.Tasks;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This class handles the Emoji Hub Button. Tapping on it opens up 5 Emoji's the player can select from during
	/// an adventure.
	/// These Emoji could change depending on what the player wants to equip there due to IAPs / Rewards, so their sprite
	/// is loaded dynamically.
	/// </summary>
	public class EmojiHubButtonView : MonoBehaviour
	{
		[SerializeField, Required] private Button _emojiHubButton;
		[SerializeField] private EmojiButtonView [] _emojiButtonViews;
		[SerializeField, Required] protected Animation _animation;
		[SerializeField, Required] protected AnimationClip _introAnimationClip;
		[SerializeField, Required] protected AnimationClip _outroAnimationClip;

		private void Awake()
		{
			_emojiHubButton.onClick.AddListener(OnEmojiHubButtonPressed);
		}

		private void Start()
		{
			var emojis = GameIdGroup.Emoji.GetIds();

			for (var i = 0; i < _emojiButtonViews.Length; i++)
			{
				if (i < emojis.Count)
				{
					_emojiButtonViews[i].Init(emojis[i]);
				}
				
				_emojiButtonViews[i].gameObject.SetActive(false);
				_emojiButtonViews[i].EmojiButton.onClick.AddListener(PlayDisappearAnimations);
			}
		}
		
		private void OnEmojiHubButtonPressed()
		{
			if (_emojiButtonViews[0].gameObject.activeSelf)
			{
				PlayDisappearAnimations();
			}
			else
			{
				PlayAppearAnimations();
			}
		}

		private void PlayAppearAnimations()
		{
			foreach (var button in _emojiButtonViews)
			{
				button.SetActive(true);
			}
			
			_animation.clip = _introAnimationClip;
			_animation.Play();
		}
		
		private async void PlayDisappearAnimations()
		{
			_animation.clip = _outroAnimationClip;
			_animation.Play();
			
			await Task.Delay(Mathf.RoundToInt(_animation.clip.length * 1000));
			
			foreach (var button in _emojiButtonViews)
			{
				button.SetActive(false);
			}
		}
	}
}

