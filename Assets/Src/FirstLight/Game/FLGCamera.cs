using Cinemachine;
using FirstLight.Game.Core;
using UnityEngine;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main camera of the game
	/// </summary>
	public class FLGCamera : MonoSingleton<FLGCamera>
	{
		[SerializeField] private Camera _camera;
		[SerializeField] private CinemachineBrain _cinemachineBrain;
		
		public Camera MainCamera => _camera;

		public CinemachineBrain CinemachineBrain => _cinemachineBrain;
		
		protected override void _Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}