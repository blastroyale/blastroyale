using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Adventure
{
	/// <summary>
	/// This Mono component controls the stadium tv mesh rendering behaviour 
	/// </summary>
	public class StadiumTvSimulationMonoComponent : MonoBehaviour
	{
		[Serializable]
		public struct StadiumMaterial
		{
			public Material Material;
			public float ShowTime;
		}
		
		[SerializeField] private Renderer _mainRenderer;
		[SerializeField] private StadiumMaterial[] _stadiumScreenMaterials;
		[SerializeField] private StandingsHolderView _standings;
		
		private IGameServices _services;
		private int _materialIndex;
		private float _currentShowTime;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnGameCompleted);
			
			_services.TickService.SubscribeOnUpdate(UpdateTick);

		}

		private void OnDestroy()
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}

		private void UpdateTick(float deltaTime)
		{
			var showTime = _stadiumScreenMaterials[_materialIndex].ShowTime;

			_currentShowTime += deltaTime;
			
			if (_currentShowTime < showTime)
			{
				return;
			}

			_materialIndex++;

			if (_materialIndex >= _stadiumScreenMaterials.Length)
			{
				_materialIndex = 0;
			}

			_currentShowTime = 0;

			_mainRenderer.material = _stadiumScreenMaterials[_materialIndex].Material;
		}
		
		private void OnGameCompleted(EventOnGameEnded callback)
		{
			var frame = callback.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = new List<QuantumPlayerMatchData>();

			for(var i = 0; i < container.PlayersData.Length; i++)
			{
				if (!container.PlayersData[i].IsValid)
				{
					continue;
				}
				
				playerData.Add(new QuantumPlayerMatchData(frame,container.PlayersData[i]));
			}

			_standings.Initialise(playerData);
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}
	}
}