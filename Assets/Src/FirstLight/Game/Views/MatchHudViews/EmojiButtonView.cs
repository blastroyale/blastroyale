using System;
using DG.Tweening;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This class handles individual Emoji buttons. Tapping on one creates an Emoji above the player's head, which is broadcast
	/// to all players through Quantum.
	/// </summary>
	public class EmojiButtonView : MonoBehaviour
	{
		public Button EmojiButton;
		
		[SerializeField] private Image _emojiImage;

		private IGameServices _services;
		private GameId _emoji;

		private void OnValidate()
		{
			EmojiButton = EmojiButton ? EmojiButton : GetComponent<Button>();
			_emojiImage = _emojiImage ? _emojiImage : GetComponent<Image>();
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			EmojiButton.onClick.AddListener(OnButtonPress);
		}

		/// <summary>
		/// Initialises this Emoji Button's image with the given <paramref name="emoji"/> sprite
		/// </summary>
		public async void Init(GameId emoji)
		{
			_emoji = emoji;
			_emojiImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(emoji);
		}
		
		private void OnButtonPress()
		{
			QuantumRunner.Default.Game.SendCommand(new PlayerEmojiCommand { Emoji = _emoji });
		}

		/// <summary>
		/// Plays the emoji button disappear Animation
		/// </summary>
		public void SetActive(bool active)
		{
			this.Validate<EmojiView>()?.gameObject.SetActive(active);
		}
	}
}

