using Cinemachine;
using FirstLight.Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main camera of the game
	/// </summary>
	public class FLGCamera : MonoSingleton<FLGCamera>
	{
		[SerializeField, Required] private Camera _camera;
		[SerializeField, Required] private CinemachineBrain _cinemachineBrain;
		[SerializeField, Required] private PhysicsRaycaster _physicsRaycaster;
		
		public Camera MainCamera => _camera;

		public PhysicsRaycaster PhysicsRaycaster => _physicsRaycaster;
		
		public CinemachineBrain CinemachineBrain => _cinemachineBrain;
		
		protected override void _Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}