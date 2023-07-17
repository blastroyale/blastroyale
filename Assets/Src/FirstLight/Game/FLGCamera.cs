using Cinemachine;
using FirstLight.Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main camera of the game
	/// </summary>
	public class FLGCamera : MonoSingleton<FLGCamera>
	{
		[SerializeField, Required] private Camera _camera;
		[SerializeField, Required] private CinemachineBrain _cinemachineBrain;
		
		public Camera MainCamera => _camera;

		public CinemachineBrain CinemachineBrain => _cinemachineBrain;
		
		protected override void _Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}