
using System;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display how many contenders are left within the Battle Royale via a message. IE "10 PLAYERS REMAINING".
	/// </summary>
	public class ContendersLeftHolderMessageView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _contendersLeftText;
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _animationClipFadeIn;
		[SerializeField] private AnimationClip _animationClipFadeOut;

		private IGameServices _services;
		private int _playersLeft;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
			_contendersLeftText.text = "";
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out _);

			_playersLeft = matchData.Length;
		}
		
		/// <summary>
		/// The scoreboard could update whilst it's open, e.g. players killed whilst looking at it, etc.
		/// </summary>
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var deadData = callback.PlayersMatchData[callback.PlayerDead];
			
			_playersLeft--;
			if (_playersLeft < 1)
			{
				_playersLeft = 1;
			}

			Debug.Log("Player Rank: " + (deadData.PlayerRank - 1) );
			
			_contendersLeftText.text = _playersLeft.ToString() + " PLAYERS REMAINING";
				// (deadData.PlayerRank - 1) + " PLAYERS REMAINING";
			
			_animation.clip = _animationClipFadeIn;
			_animation.Rewind();
			_animation.Play();
			
			this.LateCall(_animation.clip.length, PlayFadeOutAnimation);
		}
		
		private void PlayFadeOutAnimation()
		{
			_animation.clip = _animationClipFadeOut;
			_animation.Rewind();
			_animation.Play();
		}
	}
}
