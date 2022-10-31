﻿using System;
using System.Collections;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Handles logic for the Map Timer in Battle Royale mode.
	/// </summary>
	public class MapTimerView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _mapStatusText;
		[SerializeField, Required] private GameObject _timerHolder;
		[SerializeField, Required] private TextMeshProUGUI _timerText;
		[SerializeField, Required] private Animation _mapStatusTextAnimation;
		[SerializeField, Required] private GameObject _timerOutline;
		[SerializeField, Required] private Animation _mapShrinkingTimerAnimation;

		[SerializeField, Title("Colors")] private Color _safeZoneStatusColor = Color.white;
		[SerializeField, Title("Colors")] private Color _areaShrinkingStatusColor = Color.red;

		private IGameServices _services;
		private DateTime _timerUpdatingUntil;

		private Coroutine _timerCoroutine;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_services.MessageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnded);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
		}

		private void OnMatchEnded(MatchEndedMessage msg)
		{
			if (_timerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_timerCoroutine);
			}
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);

			if (_timerCoroutine != null)
			{
				_services?.CoroutineService?.StopCoroutine(_timerCoroutine);
			}
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || _timerCoroutine != null)
			{
				return;
			}

			var frame = QuantumRunner.Default.Game.Frames.Verified;

			if (frame.TryGetSingleton<ShrinkingCircle>(out _))
			{
				StartTimerCoroutine(frame);
			}
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var frame = message.Game.Frames.Predicted;
			
			if (frame.TryGetSingleton<ShrinkingCircle>(out _))
			{
				StartTimerCoroutine(frame);
			}
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			StartTimerCoroutine(callback.Game.Frames.Predicted);
		}

		private void StartTimerCoroutine(Frame f)
		{
			if (_timerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_timerCoroutine);
			}
			
			_timerCoroutine = _services.CoroutineService.StartCoroutine(UpdateShrinkingCircleTimer(f));
		}

		private IEnumerator UpdateShrinkingCircleTimer(Frame f)
		{
			var circle = f.GetSingleton<ShrinkingCircle>();

			while (circle.Step < 0)
			{
				yield return null;
			}
			
			var config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
			var time = (circle.ShrinkingStartTime - f.Time - config.WarningTime).AsFloat;

			_mapStatusText.gameObject.SetActive(true);
			_mapStatusText.text = ScriptLocalization.AdventureMenu.GetReady;
			_mapStatusText.color = _safeZoneStatusColor;
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();

			yield return new WaitForSeconds(time);
			
			time = Time.time + (circle.ShrinkingStartTime - QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;
			_mapStatusText.text = ScriptLocalization.AdventureMenu.GoToArea;

			_timerHolder.SetActive(true);
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();

			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");

				yield return null;
			}

			_mapStatusText.text = ScriptLocalization.AdventureMenu.AreaShrinking;
			_mapStatusText.color = _areaShrinkingStatusColor;
			time = Time.time + (circle.ShrinkingStartTime + circle.ShrinkingDurationTime -
			                    QuantumRunner.Default.Game.Frames.Predicted.Time).AsFloat;

			_timerOutline.SetActive(true);
			_mapStatusTextAnimation.Rewind();
			_mapStatusTextAnimation.Play();
			_mapShrinkingTimerAnimation.Rewind();
			_mapShrinkingTimerAnimation.Play();

			while (Time.time < time)
			{
				_timerText.text = (time - Time.time).ToString("N0");

				yield return null;
			}

			_timerHolder.SetActive(false);
			_timerOutline.SetActive(false);
			_mapStatusText.gameObject.SetActive(false);
		}
	}
}